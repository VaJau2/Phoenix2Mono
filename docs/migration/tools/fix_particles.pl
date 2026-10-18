#!/usr/bin/env perl
# Этап 5 миграции: частицы Godot 3 (Particles + ParticlesMaterial) -> Godot 4 (GPUParticles3D +
# ParticleProcessMaterial).
#
# 1. Параметры со случайностью. В Godot 3 — значение X и разброс r (scene/resources/particles_material.cpp, 3.6):
#      масштаб:          mix(scale, 1.0, scale_random * rand)         -> [scale, scale + (1 - scale) * r]
#      угловая скорость: X * mix(1.0, rand * 2 - 1, r)                 -> [X * (1 - 2r), X]
#      остальные:        X * mix(1.0, rand, r)                         -> [X * (1 - r), X]
#    В Godot 4 — диапазон <параметр>_min/_max. Конвертер переводил масштаб как [X * (1 - r), X] (к нулю,
#    а не к 1) — частицы с масштабом 0.5 и разбросом 1 становились от 0 до 0.5 вместо 0.5–1.
#    Диапазоны пересчитываются из значений Godot 3 (коммит 6c2d3229) для всех параметров.
# 2. Значения по умолчанию узла: local_coords в Godot 3 — true, в Godot 4 — false (частицы движущегося
#    источника — дым из ствола и т. п. — оставались на месте в мире); fixed_fps в Godot 3 — 0 (каждый кадр),
#    в Godot 4 — 30. Пишутся значения Godot 3.
# Не переносится: trail_divisor (в Godot 4 другая система следов).
#
# Сопоставление: материалы — по id ресурса (конвертер их сохраняет), узлы — по пути в сцене.
# Запуск (Godot закрыт): perl docs/migration/tools/fix_particles.pl [файлы...]
# без файлов — все .tscn/.tres, где есть GPUParticles3D или ParticleProcessMaterial.
use strict;
use warnings;
use File::Find;

my $G3 = '6c2d3229';
my @files = @ARGV;
if (!@files) {
    find(sub {
        if ($_ eq '.godot') { $File::Find::prune = 1; return }
        return unless /\.(tscn|tres)$/;
        open(my $h, '<:raw', $_) or return;
        my $t = do { local $/; <$h> };
        push @files, $File::Find::name =~ s{^\./}{}r if $t =~ /type="(GPUParticles3D|ParticleProcessMaterial)"/;
    }, '.');
}

# параметр Godot 3 -> (имя в Godot 4, разброс в Godot 3, значение по умолчанию, вид формулы)
my %params = (
    initial_velocity => ['initial_velocity', 'initial_velocity_random', 0, 'mul'],
    angular_velocity => ['angular_velocity', 'angular_velocity_random', 0, 'signed'],
    orbit_velocity   => ['orbit_velocity',   'orbit_velocity_random',   0, 'mul'],
    linear_accel     => ['linear_accel',     'linear_accel_random',     0, 'mul'],
    radial_accel     => ['radial_accel',     'radial_accel_random',     0, 'mul'],
    tangential_accel => ['tangential_accel', 'tangential_accel_random', 0, 'mul'],
    damping          => ['damping',          'damping_random',          0, 'mul'],
    angle            => ['angle',            'angle_random',            0, 'mul'],
    scale            => ['scale',            'scale_random',            1, 'scale'],
    hue_variation    => ['hue_variation',    'hue_variation_random',    0, 'signed'],
    anim_speed       => ['anim_speed',       'anim_speed_random',       0, 'mul'],
    anim_offset      => ['anim_offset',      'anim_offset_random',      0, 'mul'],
);

my ($nmat, $nnode) = (0, 0);
for my $file (sort @files) {
    my $g3 = `git show $G3:$file 2>/dev/null`;
    next unless $g3;
    my %g3mat = g3_blocks($g3, qr/ParticlesMaterial/);
    my %g3node = g3_nodes($g3);

    open(my $in, '<:raw', $file) or die "$file: $!";
    my $text = do { local $/; <$in> };
    close $in;
    my @blocks = split /(?=^\[)/m, $text;
    my $changed = 0;
    for my $b (@blocks) {
        if ($b =~ /^\[sub_resource type="ParticleProcessMaterial" id="([^"]+)"\]/ || ($b =~ /^\[resource\]/ && $text =~ /^\[gd_resource type="ParticleProcessMaterial"/)) {
            my $id = $1 // 'resource';
            my $p = $g3mat{$id} or next;
            for my $k (sort keys %params) {
                my ($name4, $rand_key, $def, $kind) = @{ $params{$k} };
                next unless exists $p->{$k} || exists $p->{$rand_key};
                my $x = $p->{$k} // $def;
                my $r = $p->{$rand_key} // 0;
                my ($a, $c) = $kind eq 'scale' ? ($x, $x + (1 - $x) * $r)
                            : $kind eq 'signed' ? ($x * (1 - 2 * $r), $x)
                            : ($x * (1 - $r), $x);
                ($a, $c) = ($c, $a) if $a > $c;
                $b =~ s/^${name4}_(min|max) = .*\n//mg;
                $b =~ s/\n*\z//;
                $b .= sprintf("\n%s_min = %.6g\n%s_max = %.6g\n\n", $name4, $a, $name4, $c);
            }
            $nmat++;
            $changed++;
        }
        elsif ($b =~ /^\[node name="([^"]+)" type="GPUParticles3D"(?: parent="([^"]*)")?/) {
            my $path = defined $2 ? ($2 eq '.' ? $1 : "$2/$1") : '.';
            my $p = $g3node{$path} or next;
            my $local = $p->{local_coords} // 'true';
            my $fps = $p->{fixed_fps} // 0;
            $b =~ s/^(local_coords|fixed_fps) = .*\n//mg;
            $b =~ s/\n*\z//;
            $b .= "\nlocal_coords = $local\nfixed_fps = $fps\n\n";
            $nnode++;
            $changed++;
        }
    }
    next unless $changed;
    open(my $out, '>:raw', $file) or die "$file: $!";
    print $out join('', @blocks);
    close $out;
    print "$file\n";
}
print "материалов: $nmat, узлов: $nnode\n";

# свойства блоков Godot 3 заданного типа: id -> { свойство -> значение }
sub g3_blocks {
    my ($text, $type) = @_;
    my %r;
    for my $b (split /(?=^\[)/m, $text) {
        my $id;
        if ($b =~ /^\[sub_resource type="$type" id=(\d+)\]/) { $id = $1 }
        elsif ($b =~ /^\[resource\]/ && $text =~ /^\[gd_resource type="$type"/) { $id = 'resource' }
        else { next }
        $r{$id} = props($b);
    }
    return %r;
}

sub g3_nodes {
    my $text = shift;
    my %r;
    for my $b (split /(?=^\[)/m, $text) {
        next unless $b =~ /^\[node name="([^"]+)" type="Particles"(?: parent="([^"]*)")?/;
        my $path = defined $2 ? ($2 eq '.' ? $1 : "$2/$1") : '.';
        $r{$path} = props($b);
    }
    return %r;
}

sub props {
    my %p;
    for (split /\n/, shift) {
        $p{$1} = $2 if /^(\w+) = ([-\w.]+)$/;
    }
    return \%p;
}
