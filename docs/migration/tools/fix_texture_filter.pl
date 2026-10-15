#!/usr/bin/env perl
# Этап 3 миграции: фильтрация текстур в 3D.
# В Godot 3 фильтрация задавалась при импорте текстуры (в проекте flags/filter=false — пиксельный стиль),
# в Godot 4 — в материале (BaseMaterial3D.texture_filter, по умолчанию LINEAR_WITH_MIPMAPS) и у Sprite3D.
# Всем StandardMaterial3D/ORMMaterial3D и узлам Sprite3D ставится texture_filter = 0 (Nearest),
# кроме материалов с текстурами, у которых фильтрация в Godot 3 была включена (snow.tres).
# Идемпотентен (не трогает секции, где texture_filter уже задан).
# Запуск из корня репозитория: perl docs/migration/tools/fix_texture_filter.pl
use strict; use warnings;
my %skip = map { $_ => 1 } ('assets/materials/props/locations/snow.tres');
my @files;
{ open(my $git, '-|', 'git', 'ls-files', '-z', '*.tscn', '*.tres') or die $!; local $/ = "\0"; while (my $f = <$git>) { chomp $f; push @files, $f } }
my %stat;
for my $f (@files) {
    next if $skip{$f};
    open(my $fh, '<:raw', $f) or die "$f: $!"; local $/; my $t = <$fh>; close $fh;
    my $o = $t;
    my $gd_mat = $t =~ /^\[gd_resource type="(?:StandardMaterial3D|ORMMaterial3D)"/;
    my @sec = split /^(?=\[)/m, $t;
    for (@sec) {
        my $is_mat = /^\[sub_resource type="(?:StandardMaterial3D|ORMMaterial3D)"/ || ($gd_mat && /^\[resource\]/);
        my $is_sprite = /^\[node [^\n]*type="Sprite3D"/;
        next unless $is_mat || $is_sprite;
        next if /^texture_filter = /m;
        s/^(\[[^\n]*\]\n)/$1texture_filter = 0\n/;
        $stat{$is_mat ? 'materials' : 'sprites'}++;
    }
    $t = join '', @sec;
    if ($t ne $o) { open(my $w, '>:raw', $f) or die; print $w $t; close $w }
}
print "DONE ", join(' ', map { "$_=$stat{$_}" } sort keys %stat), "\n";
