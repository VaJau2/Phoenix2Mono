#!/usr/bin/env perl
# Исправляет комментарии Vorbis без '=' в .ogg (Godot 4 при каждой загрузке пишет
# "Invalid comment in Ogg Vorbis file ..., should contain '='"). Такой комментарий
# получает префикс "ENCODER=" (в найденных файлах это строка кодировщика Sony).
# Звук не перекодируется: пересобираются только страницы с заголовками Vorbis,
# у остальных страниц меняются номер и CRC, если число заголовочных страниц изменилось.
# Запуск: perl docs/migration/tools/fix_ogg_comments.pl [--dry-run] <файлы .ogg>
#   (все файлы проекта: find . -name "*.ogg" -not -path "./.godot/*" -print0 | xargs -0 perl ...)
# После правки Godot должен переимпортировать файлы (открыть редактор или запустить с --import).
use strict;
use warnings;

my $dry = grep { $_ eq '--dry-run' } @ARGV;
my @files = grep { $_ ne '--dry-run' } @ARGV;

# CRC Ogg: полином 0x04C11DB7, без отражения, начальное значение 0
my @crc_table;
for my $i (0 .. 255) {
    my $r = $i << 24;
    for (1 .. 8) { $r = ($r & 0x80000000) ? (($r << 1) ^ 0x04C11DB7) : ($r << 1); $r &= 0xFFFFFFFF; }
    push @crc_table, $r;
}

sub ogg_crc {
    my ($data) = @_;
    my $crc = 0;
    $crc = (($crc << 8) & 0xFFFFFFFF) ^ $crc_table[(($crc >> 24) ^ $_) & 0xFF] for unpack('C*', $data);
    return $crc;
}

# страница: заголовок (без lacing) + сегменты; CRC считается с нулевым полем CRC
sub build_page {
    my ($flags, $granule, $serial, $seq, $lacing, $body) = @_;
    my $page = 'OggS' . pack('CC', 0, $flags) . $granule . pack('VVV', $serial, $seq, 0)
        . pack('C', scalar @$lacing) . pack('C*', @$lacing) . $body;
    substr($page, 22, 4) = pack('V', ogg_crc($page));
    return $page;
}

sub parse_pages {
    my ($d) = @_;
    my ($pos, @pages) = (0);
    while ($pos < length $d) {
        return undef if substr($d, $pos, 4) ne 'OggS';
        my $ns = ord substr($d, $pos + 26, 1);
        my @lac = unpack('C*', substr($d, $pos + 27, $ns));
        my $blen = 0;
        $blen += $_ for @lac;
        my $hl = 27 + $ns;
        push @pages, {
            flags   => ord(substr($d, $pos + 5, 1)),
            granule => substr($d, $pos + 6, 8),
            serial  => unpack('V', substr($d, $pos + 14, 4)),
            seq     => unpack('V', substr($d, $pos + 18, 4)),
            lacing  => \@lac,
            body    => substr($d, $pos + $hl, $blen),
            raw     => substr($d, $pos, $hl + $blen),
        };
        $pos += $hl + $blen;
    }
    return \@pages;
}

# возвращает новый пакет комментариев или undef, если править нечего
sub fix_comment_packet {
    my ($c, $file) = @_;
    return undef unless substr($c, 0, 7) eq "\x03vorbis";
    my $o = 7;
    my $vl = unpack('V', substr($c, $o, 4));
    my $vendor = substr($c, $o + 4, $vl);
    $o += 4 + $vl;
    my $n = unpack('V', substr($c, $o, 4));
    $o += 4;
    my (@comments, $changed);
    for (1 .. $n) {
        my $l = unpack('V', substr($c, $o, 4));
        my $s = substr($c, $o + 4, $l);
        $o += 4 + $l;
        if ($s !~ /=/) {
            print "$file: \"$s\" -> \"ENCODER=$s\"\n";
            $s = "ENCODER=$s";
            $changed = 1;
        }
        push @comments, $s;
    }
    return undef unless $changed;
    my $rest = substr($c, $o);   # байт framing (1)
    return "\x03vorbis" . pack('V', length $vendor) . $vendor . pack('V', scalar @comments)
        . join('', map { pack('V', length $_) . $_ } @comments) . $rest;
}

for my $file (@files) {
    open my $fh, '<:raw', $file or die "$file: $!";
    my $d = do { local $/; <$fh> };
    close $fh;

    my $pages = parse_pages($d);
    if (!$pages) { warn "$file: не Ogg, пропущен\n"; next; }
    if (grep { $_->{serial} != $pages->[0]{serial} } @$pages) { warn "$file: несколько потоков, пропущен\n"; next; }

    # три заголовочных пакета Vorbis; третий должен закончиться вместе со страницей
    my @packets;
    my $cur = '';
    my $last_header_page;
    for my $i (0 .. $#$pages) {
        my $p = $pages->[$i];
        my $off = 0;
        for my $l (@{ $p->{lacing} }) {
            die "$file: аудио на странице заголовков\n" if @packets == 3;
            $cur .= substr($p->{body}, $off, $l);
            $off += $l;
            if ($l < 255) { push @packets, $cur; $cur = ''; }
        }
        if (@packets == 3) { $last_header_page = $i; last; }
    }
    if (!defined $last_header_page || $packets[0] !~ /^\x01vorbis/) { warn "$file: не Vorbis, пропущен\n"; next; }

    my $new_comment = fix_comment_packet($packets[1], $file);
    next unless $new_comment;
    next if $dry;

    # страница 0 (идентификация) — без изменений; пакеты 2–3 раскладываются по страницам заново
    my @out = ($pages->[0]{raw});
    my $serial = $pages->[0]{serial};
    my @segs;   # [длина сегмента, конец пакета?]
    my $stream = '';
    for my $pk ($new_comment, $packets[2]) {
        my $len = length $pk;
        while ($len >= 255) { push @segs, [255, 0]; $len -= 255; }
        push @segs, [$len, 1];
        $stream .= $pk;
    }
    my ($seq, $off, $continued) = (1, 0, 0);
    while (@segs) {
        my @chunk = splice(@segs, 0, 255);
        my @lac = map { $_->[0] } @chunk;
        my $blen = 0;
        $blen += $_ for @lac;
        my $ends_packet = grep { $_->[1] } @chunk;
        my $granule = $ends_packet ? pack('VV', 0, 0) : pack('VV', 0xFFFFFFFF, 0xFFFFFFFF);
        push @out, build_page($continued ? 1 : 0, $granule, $serial, $seq++, \@lac, substr($stream, $off, $blen));
        $off += $blen;
        $continued = !$chunk[-1][1];
    }

    # страницы со звуком — тот же контент, новый номер и CRC
    my $shift = $seq - ($last_header_page + 1);
    for my $i ($last_header_page + 1 .. $#$pages) {
        my $p = $pages->[$i];
        push @out, $shift ? build_page($p->{flags}, $p->{granule}, $serial, $p->{seq} + $shift, $p->{lacing}, $p->{body})
                          : $p->{raw};
    }

    open my $oh, '>:raw', $file or die "$file: $!";
    print $oh join('', @out);
    close $oh;
}
