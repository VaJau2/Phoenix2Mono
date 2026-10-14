#!/usr/bin/env perl
# Этап 3 миграции: шрифты Godot 3 (DynamicFont) -> Godot 4.
#
# Конвертер Godot переименовал DynamicFont в FontFile, оставив свойства font_data/size —
# в Godot 4 их у FontFile нет (FontFile — это сам .ttf), и размер у всех надписей становится 16.
# Здесь:
#   - старый DynamicFont (ресурс с font_data) -> FontVariation с base_font = font_data;
#   - size переносится в места использования:
#       default_font = F                -> + default_font_size = S          (Theme)
#       Тип/fonts/<имя> = F             -> + Тип/font_sizes/<имя>_size = S  (Theme)
#       theme_override_fonts/<имя> = F  -> + theme_override_font_sizes/<имя>_size = S (узлы)
#     где для <имя> = font размер называется font_size (Label, Button, ...),
#     для RichTextLabel: normal_font -> normal_font_size и т. д.
# Если size не задан, это 16 — как и по умолчанию в Godot 4, ничего не дописывается.
#
# Запуск из корня репозитория: perl docs/migration/tools/fix_fonts.pl
use strict;
use warnings;

my @files;
{
    open(my $git, '-|', 'git', 'ls-files', '-z', '*.tscn', '*.tres') or die $!;
    local $/ = "\0";
    while (my $f = <$git>) { chomp $f; push @files, $f; }
}

sub slurp { open(my $fh, '<:raw', $_[0]) or die "$_[0]: $!"; local $/; my $t = <$fh>; close $fh; $t }
sub spit { open(my $fh, '>:raw', $_[0]) or die "$_[0]: $!"; print $fh $_[1]; close $fh }

# 1. Файлы .tres, которые были DynamicFont: путь -> размер
my %font_files;
for my $f (@files) {
    next unless $f =~ /\.tres$/;
    my $t = slurp($f);
    next unless $t =~ /^\[gd_resource type="FontFile"/ && $t =~ /^font_data = /m;
    my ($size) = $t =~ /^size = (\d+)/m;
    $font_files{"res://$f"} = $size // 16;
}

my %stat = (fonts => 0, sizes => 0);

for my $f (@files) {
    my $t = slurp($f);
    my $orig = $t;

    # ext_resource id -> размер (для ссылок на старые .tres шрифты)
    my %size_of;
    $t =~ s{^\[ext_resource path="([^"]+)" type="FontFile" id=(\S+?)\]}{
        my ($p, $id) = ($1, $2);
        if (exists $font_files{$p}) { $size_of{"ExtResource( $id )"} = $font_files{$p}; "[ext_resource path=\"$p\" type=\"FontVariation\" id=$id]" }
        else { "[ext_resource path=\"$p\" type=\"FontFile\" id=$id]" }
    }mge;

    # sub_resource старых шрифтов -> FontVariation
    $t =~ s{^\[sub_resource type="FontFile" id=(\S+?)\]\n((?:[^\[\n][^\n]*\n)*)}{
        my ($id, $body) = ($1, $2);
        if ($body =~ /^font_data = /m) {
            my ($size) = $body =~ /^size = (\d+)/m;
            $size_of{"SubResource( $id )"} = $size // 16;
            $stat{fonts}++;
            "[sub_resource type=\"FontVariation\" id=$id]\n" . convert_body($body);
        } else { "[sub_resource type=\"FontFile\" id=$id]\n$body" }
    }mge;

    # сам файл — старый DynamicFont
    if ($t =~ /^\[gd_resource type="FontFile"/ && $t =~ /^font_data = /m) {
        $t =~ s/^\[gd_resource type="FontFile"/[gd_resource type="FontVariation"/;
        $t =~ s{^\[resource\]\n((?:[^\[\n][^\n]*\n?)*)}{"[resource]\n" . convert_body($1)}me;
        $stat{fonts}++;
    }

    # размеры в местах использования
    if (%size_of) {
        my $refs = join '|', map { quotemeta } keys %size_of;
        $t =~ s{^((?:default_font|[\w]+/fonts/\w+|theme_override_fonts/\w+)) = ($refs)\n}{
            my ($key, $ref) = ($1, $2);
            my $size = $size_of{$ref};
            my $line = "$key = $ref\n";
            if ($size != 16) {
                $line .= size_key($key) . " = $size\n";
                $stat{sizes}++;
            }
            $line;
        }mge;
    }

    spit($f, $t) if $t ne $orig;
}
print "DONE ", join(' ', map { "$_=$stat{$_}" } sort keys %stat), "\n";

# Свойства DynamicFont -> FontVariation (размер и обводка в FontVariation не задаются)
sub convert_body {
    my ($body) = @_;
    my @out;
    for (split /\n/, $body) {
        if (/^font_data = (.*)$/) { push @out, "base_font = $1" }
        elsif (/^extra_spacing_(top|bottom|char|space) = (.*)$/) {
            my %map = (top => 'spacing_top', bottom => 'spacing_bottom', char => 'spacing_glyph', space => 'spacing_space');
            push @out, "$map{$1} = $2";
        }
        elsif (/^(size|outline_size|outline_color|use_filter|use_mipmaps|fallback\/\d+) = /) { next }
        elsif (/\S/) { push @out, $_ }
    }
    return join('', map { "$_\n" } @out) . "\n";
}

sub size_key {
    my ($key) = @_;
    return 'default_font_size' if $key eq 'default_font';
    return "$1/font_sizes/" . ($2 eq 'font' ? 'font_size' : "${2}_size") if $key =~ m{^(\w+)/fonts/(\w+)$} && $1 ne 'theme_override_fonts';
    return 'theme_override_font_sizes/' . ($1 eq 'font' ? 'font_size' : "${1}_size") if $key =~ m{^theme_override_fonts/(\w+)$};
    die "unexpected key $key";
}
