#!/usr/bin/env perl
# Этап 4 миграции: пути свойств в анимациях (NodePath "узел:свойство:подсвойство").
#   ...:mesh:surface_N/...  — в Godot 3 поверхности нумеровались с 1 (surface_1 — первая), в Godot 4 с 0
#   ...:material/N...       — переопределение материала узла, в Godot 4 surface_material_override/N
# Без этого анимации цвета/свечения материалов (призраки) не работают и сыплют ошибками каждый кадр.
# НЕ идемпотентен (surface_N уменьшается) — запускать один раз.
# Запуск из корня репозитория: perl docs/migration/tools/fix_track_paths.pl
use strict; use warnings;
my @files;
{ open(my $git, '-|', 'git', 'ls-files', '-z', '*.tscn', '*.tres') or die $!; local $/ = "\0"; while (my $f = <$git>) { chomp $f; push @files, $f } }
my %stat;
for my $f (@files) {
    open(my $fh, '<:raw', $f) or die "$f: $!"; local $/; my $t = <$fh>; close $fh;
    my $o = $t;
    $t =~ s{(NodePath\("[^"]*:mesh:surface_)(\d+)(/)}{$stat{surface}++; $1 . ($2 - 1) . $3}ge;
    $t =~ s{(NodePath\("[^"]*):material/(\d+)([:"])}{$stat{material}++; "$1:surface_material_override/$2$3"}ge;
    if ($t ne $o) { open(my $w, '>:raw', $f) or die; print $w $t; close $w; $stat{files}++ }
}
print "DONE ", join(' ', map { "$_=$stat{$_}" } sort keys %stat), "\n";
