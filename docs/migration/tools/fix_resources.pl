#!/usr/bin/env perl
# Этап 3 миграции: свойства ресурсов Godot 3, которые Godot 4.7 молча теряет
# (проверено тестовыми ресурсами, см. docs/migration/plan.md, этап 3).
#
# ParticleProcessMaterial (бывший ParticlesMaterial):
#   X + X_random  ->  X_min = X * (1 - X_random), X_max = X
#     (в Godot 3 значение бралось случайно между X*(1-random) и X)
#   flag_disable_z / flag_align_y / flag_rotate_y -> particle_flag_*; trail_* удаляются
# StandardMaterial3D:
#   params_depth_draw_mode = 1 (ALWAYS)            -> depth_draw_mode = 1
#   params_depth_draw_mode = 3 (ALPHA_OPAQUE_PREPASS) -> transparency = 4 (ALPHA_DEPTH_PRE_PASS)
# Sky (бывшие ProceduralSky / PanoramaSky): параметры неба переносятся в новый
#   ProceduralSkyMaterial / PanoramaSkyMaterial, Sky ссылается на него через sky_material.
#   Солнце в Godot 4 рисуется от DirectionalLight3D, sun_* параметры удаляются.
# Environment: background_energy -> background_energy_multiplier, fog_color -> fog_light_color,
#   fog_sun_amount -> fog_sun_scatter (остальной туман/DOF — на этапе 5, модель тумана другая).
# AnimationPlayer (узлы): playback_speed -> speed_scale.
#
# НЕ идемпотентен — запускать один раз.
# Запуск из корня репозитория: perl docs/migration/tools/fix_resources.pl
use strict;
use warnings;

my @files;
{
    open(my $git, '-|', 'git', 'ls-files', '-z', '*.tscn', '*.tres') or die $!;
    local $/ = "\0";
    while (my $f = <$git>) { chomp $f; push @files, $f; }
}

my @ranged = qw(initial_velocity angular_velocity orbit_velocity linear_accel radial_accel
                tangential_accel damping angle scale hue_variation anim_speed anim_offset);
my %g3_default = (scale => 1);
my %stat;
our %t_sky_prefix;   # .tres с gd_resource type="Sky": sub_resource материала вставляется перед [resource]

sub num { my $v = sprintf('%.6g', $_[0]); $v }

for my $f (@files) {
    open(my $fh, '<:raw', $f) or die "$f: $!";
    local $/;
    my $t = <$fh>;
    close $fh;
    my $orig = $t;

    my ($gd_type) = $t =~ /^\[gd_resource type="(\w+)"/;
    my $max_id = 0;
    $max_id = $_ > $max_id ? $_ : $max_id for $t =~ /^\[sub_resource type="\w+" id=(\d+)\]/mg;

    # Разбор на секции: заголовок + тело
    my @sections = split /^(?=\[)/m, $t;
    my @out;
    for my $sec (@sections) {
        my ($hdr) = $sec =~ /^(\[[^\n]*\])/;
        my $type = '';
        if (defined $hdr) {
            if ($hdr =~ /^\[(?:sub_resource|node) .*?type="(\w+)"/) { $type = $1 }
            elsif ($hdr eq '[resource]' && $gd_type) { $type = $gd_type }
        }

        if ($type eq 'ParticleProcessMaterial') {
            $sec = fix_particles($sec);
        }
        elsif ($type eq 'StandardMaterial3D') {
            $sec =~ s/^params_depth_draw_mode = 1$/depth_draw_mode = 1/m and $stat{depth_always}++;
            $sec =~ s/^params_depth_draw_mode = 3$/transparency = 4/m and $stat{depth_prepass}++;
        }
        elsif ($type eq 'Environment') {
            $sec =~ s/^background_energy = /background_energy_multiplier = /m and $stat{env}++;
            $sec =~ s/^fog_color = /fog_light_color = /m and $stat{env}++;
            $sec =~ s/^fog_sun_amount = /fog_sun_scatter = /m and $stat{env}++;
        }
        elsif ($type eq 'AnimationPlayer') {
            $sec =~ s/^playback_speed = /speed_scale = /m and $stat{speed_scale}++;
        }
        elsif ($type eq "Sky" && $sec !~ /^sky_material = /m && $sec =~ /^(?:sky_\w+|ground_\w+|sun_\w+|panorama) = /m) {
            my $sky_id;
            if ($hdr =~ /id=(\d+)/) { $sky_id = $1 }
            my ($mat_type, $mat_body, $sky_body) = split_sky($sec);
            if (defined $sky_id) {
                my $mat_id = ++$max_id;
                push @out, "[sub_resource type=\"$mat_type\" id=$mat_id]\n$mat_body\n";
                $sec = "$hdr\nsky_material = SubResource( $mat_id )\n$sky_body\n";
            } else {
                # .tres с gd_resource type="Sky": материал — встроенный sub_resource
                my $mat_id = ++$max_id;
                $sec = "$hdr\nsky_material = SubResource( $mat_id )\n$sky_body\n";
                $t_sky_prefix{$f} = "[sub_resource type=\"$mat_type\" id=$mat_id]\n$mat_body\n";
            }
            $stat{sky}++;
        }
        push @out, $sec;
    }
    $t = join '', @out;
    if (my $pre = delete $t_sky_prefix{$f}) { $t =~ s/^\[resource\]/$pre\[resource]/m }

    if ($t ne $orig) {
        open(my $o, '>:raw', $f) or die "$f: $!";
        print $o $t;
        close $o;
    }
}
print "DONE ", join(' ', map { "$_=$stat{$_}" } sort keys %stat), "\n";

sub fix_particles {
    my ($sec) = @_;
    my %v;
    $v{$1} = $2 while $sec =~ /^(\w+) = ([-\d.e]+)$/mg;
    for my $p (@ranged) {
        next unless exists $v{$p} || exists $v{"${p}_random"};
        my $base = exists $v{$p} ? $v{$p} : ($g3_default{$p} // 0);
        my $r = $v{"${p}_random"} // 0;
        my $new = "${p}_min = " . num($base * (1 - $r)) . "\n${p}_max = " . num($base) . "\n";
        if ($sec =~ /^\Q$p\E = [-\d.e]+\n/m) {
            $sec =~ s/^\Q$p\E = [-\d.e]+\n/$new/m;
        } else {
            $sec =~ s/\n*\z/\n$new\n/;
        }
        $sec =~ s/^\Q$p\E_random = [-\d.e]+\n//m;
        $stat{particle_ranges}++;
    }
    $sec =~ s/^flag_(disable_z|align_y|rotate_y) = /particle_flag_$1 = /mg and $stat{particle_flags}++;
    $sec =~ s/^trail_\w+ = [^\n]*\n//mg;
    return $sec;
}

# ProceduralSky / PanoramaSky -> (тип материала, тело материала, оставшееся тело Sky)
sub split_sky {
    my ($sec) = @_;
    my (@mat, @sky);
    my $panorama = $sec =~ /^panorama = /m;
    for my $line (split /\n/, $sec) {
        next if $line =~ /^\[/ || $line !~ /\S/;
        if ($line =~ /^(sky_top_color|sky_horizon_color|sky_curve|ground_bottom_color|ground_horizon_color|ground_curve|sun_angle_max|sun_curve|panorama) = /) {
            push @mat, $line;
        }
        elsif ($line =~ /^(sky_energy|ground_energy) = (.*)$/) { push @mat, "${1}_multiplier = $2" }
        elsif ($line =~ /^(sun_\w+|texture_size|sky_\w+|ground_\w+) = /) { next }   # нет аналога
        else { push @sky, $line }
    }
    return ($panorama ? 'PanoramaSkyMaterial' : 'ProceduralSkyMaterial', join("\n", @mat) . "\n", join("\n", @sky));
}
