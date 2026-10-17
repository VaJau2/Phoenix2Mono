#!/usr/bin/env perl
# Этап 3 миграции: enum'ы, у которых в Godot 4 сдвинулись значения (имя свойства понимается
# через совместимость, а число переносится как есть — и означает другое).
#   ParticleProcessMaterial.emission_shape: Godot 3 BOX=2, POINTS=3, DIRECTED_POINTS=4, RING=5
#       -> Godot 4 BOX=3, POINTS=4, DIRECTED_POINTS=5, RING=6 (в 4 добавлен SPHERE_SURFACE=2).
#       Без этого коробка эмиссии становится сферой радиуса 1, а редактор при пересохранении
#       выбрасывает emission_box_extents.
#   StandardMaterial3D params_specular_mode: Godot 3 SCHLICK_GGX=0, BLINN=1, PHONG=2, TOON=3, DISABLED=4
#       -> specular_mode Godot 4 SCHLICK_GGX=0, TOON=1, DISABLED=2 (BLINN/PHONG убраны -> GGX).
#   StandardMaterial3D params_diffuse_mode: Godot 3 BURLEY=0, LAMBERT=1, LAMBERT_WRAP=2, OREN_NAYAR=3, TOON=4
#       -> diffuse_mode Godot 4 BURLEY=0, LAMBERT=1, LAMBERT_WRAP=2, TOON=3 (OREN_NAYAR убран -> BURLEY).
# Запускать до пересохранения проекта редактором. Идемпотентен (заменяет старые имена свойств на новые).
# Запуск из корня репозитория: perl docs/migration/tools/fix_enums.pl
use strict; use warnings;
my %emission = (2 => 3, 3 => 4, 4 => 5, 5 => 6);
my %specular = (0 => 0, 1 => 0, 2 => 0, 3 => 1, 4 => 2);
my %diffuse  = (0 => 0, 1 => 1, 2 => 2, 3 => 0, 4 => 3);
my @files;
{ open(my $git, '-|', 'git', 'ls-files', '-z', '*.tscn', '*.tres') or die $!; local $/ = "\0"; while (my $f = <$git>) { chomp $f; push @files, $f } }
my %stat;
for my $f (@files) {
    open(my $fh, '<:raw', $f) or die "$f: $!"; local $/; my $t = <$fh>; close $fh;
    my $o = $t;
    my ($gd) = $t =~ /^\[gd_resource type="(\w+)"/;
    my @sec = split /^(?=\[)/m, $t;
    for (@sec) {
        my ($type) = /^\[sub_resource type="(\w+)"/;
        $type = $gd if !$type && /^\[resource\]/;
        next unless $type;
        if ($type eq 'ParticleProcessMaterial') {
            s/^emission_shape = (\d+)$/"emission_shape = " . ($emission{$1} \/\/ $1)/me and $stat{emission_shape}++;
        }
        elsif ($type eq 'StandardMaterial3D' || $type eq 'ORMMaterial3D') {
            s/^params_specular_mode = (\d+)$/"specular_mode = $specular{$1}"/me and $stat{specular}++;
            s/^params_diffuse_mode = (\d+)$/"diffuse_mode = $diffuse{$1}"/me and $stat{diffuse}++;
        }
    }
    $t = join '', @sec;
    if ($t ne $o) { open(my $w, '>:raw', $f) or die; print $w $t; close $w }
}
print "DONE ", join(' ', map { "$_=$stat{$_}" } sort keys %stat), "\n";
