#!/usr/bin/env perl
# Этап 5 миграции: окружения (Environment) и процедурное небо Godot 3 -> Godot 4.
#
# Конвертер переносит только свойства, явно записанные в файле, а значения по умолчанию в Godot 4
# другие — поэтому [resource] окружения и ProceduralSkyMaterial собираются заново из версии
# Godot 3 (коммит 6c2d3229) с учётом значений по умолчанию Godot 3:
# - туман: в Godot 3 — по глубине (begin/end/curve), сила = альфа fog_color, небо не затуманивается;
#   в Godot 4 по умолчанию экспоненциальный и ложится на небо -> fog_mode = 1 (depth),
#   fog_density = альфа, fog_sky_affect = 0; если fog_depth_end не задан, Godot 3 доводит туман
#   до дальней плоскости камеры -> fog_depth_end = 600 (дальность камеры игры; 0 в Godot 4 туман выключает);
# - рассеянный свет: в Godot 3 ambient_light_energy E умножает и цвет, и небо:
#   цвет·E·(1−c) + небо·E·c; в Godot 4 E действует только на цвет, небо входит с весом c.
#   Пересчёт: c' = c·E, E' = E·(1−c)/(1−c') (точно, пока c·E ≤ 1);
# - SSAO: в Godot 4 intensity по умолчанию 2 и power 1.5 -> 1 и 1, как в Godot 3;
# - glow: значения Godot 3 по умолчанию — blend_mode = softlight (2), intensity 0.8, уровни 3 и 5
#   (в Godot 4: screen, 0.3, уровни 2–4 с весами) — без них нет bloom;
# Значения проверены замером на тестовой сцене в Godot 3 и Godot 4 (яркость света/тени/тумана).
# - background_energy -> background_energy_multiplier, background_sky_custom_fov -> sky_custom_fov,
#   background_sky_orientation -> sky_rotation, ss_reflections_enabled -> ssr_enabled;
# - ProceduralSky: цвета и кривые по умолчанию Godot 3, sky/ground_energy -> *_energy_multiplier
#   (солнце в Godot 4 рисует DirectionalLight).
# - DOF (dof_blur_far_*) в Godot 4 — не в Environment, а в CameraAttributes: для окружений с
#   блюром создаётся <окружение>_camera.tres (CameraAttributesPractical) и подключается
#   (camera_attributes) ко всем WorldEnvironment сцен scenes/ с этим окружением.
# Не переносится: fog_transmit, fog_sun_color (в Godot 4 цвет рассеивания берётся от DirectionalLight).
#
# Запуск (Godot закрыт): perl docs/migration/tools/fix_environments.pl [файлы...]
# без файлов — assets/materials/environments/*.tres и default_env.tres.
use strict;
use warnings;

my $G3 = '6c2d3229';
my @files = @ARGV ? @ARGV : ((grep { !/_camera.tres$/ } glob('assets/materials/environments/*.tres')), 'default_env.tres');

for my $file (@files) {
    my $g3 = `git show $G3:$file`;
    die "$file: нет в $G3\n" unless $g3;
    my %env = props(section($g3, qr/^\[resource\]/));
    my %sky = $g3 =~ /type="?ProceduralSky"?/ ? props(section($g3, qr/^\[sub_resource type="?ProceduralSky"?/)) : ();

    open(my $in, '<:raw', $file) or die "$file: $!";
    my $text = do { local $/; <$in> };
    close $in;

    my ($sky_ref) = $text =~ /^sky = (SubResource\("[^"]+"\))/m;
    my @env = env_props(\%env, $sky_ref);
    $text =~ s/^\[resource\]\n.*\z/"[resource]\n" . join('', map { "$_\n" } @env)/mse;
    if (%sky || $g3 =~ /type="?ProceduralSky"?/) {
        my @sky = sky_props(\%sky);
        $text =~ s/(^\[sub_resource type="ProceduralSkyMaterial" id="[^"]+"\]\n)(?:[^\[\n][^\n]*\n|\n)*/$1 . join('', map { "$_\n" } @sky) . "\n"/me;
    }

    open(my $out, '>:raw', $file) or die "$file: $!";
    print $out $text;
    close $out;
    print "$file\n";

    camera_attributes($file, \%env) if ($env{dof_blur_far_enabled} // 'false') eq 'true';
}

# CameraAttributesPractical с дальним блюром Godot 3 + подключение к WorldEnvironment сцен
sub camera_attributes {
    my ($env_file, $e) = @_;
    (my $cam_file = $env_file) =~ s/\.tres$/_camera.tres/;
    my @p = ('dof_blur_far_enabled = true');
    push @p, 'dof_blur_far_distance = ' . ($e->{dof_blur_far_distance} // 10.0);
    push @p, 'dof_blur_far_transition = ' . ($e->{dof_blur_far_transition} // 5.0);
    push @p, "dof_blur_amount = $e->{dof_blur_far_amount}" if exists $e->{dof_blur_far_amount};
    open(my $out, '>:raw', $cam_file) or die "$cam_file: $!";
    print $out "[gd_resource type=\"CameraAttributesPractical\" format=3]\n\n[resource]\n" . join('', map { "$_\n" } @p);
    close $out;
    print "$cam_file\n";

    my $env_res = "res://$env_file";
    my $cam_res = "res://$cam_file";
    for my $scene (glob('scenes/*.tscn')) {
        open(my $in, '<:raw', $scene) or die "$scene: $!";
        my $text = do { local $/; <$in> };
        close $in;
        my ($env_id) = $text =~ /^\[ext_resource type="Environment"[^\]]*path="\Q$env_res\E" id="([^"]+)"\]/m;
        next unless defined $env_id;
        my ($cam_id) = $text =~ /^\[ext_resource type="CameraAttributesPractical"[^\]]*path="\Q$cam_res\E" id="([^"]+)"\]/m;
        if (!defined $cam_id) {
            $cam_id = 'camera_attributes_g3';
            # после последнего ext_resource
            $text =~ s/(\A(?:.*\n)*^\[ext_resource [^\n]*\n)/$1\[ext_resource type="CameraAttributesPractical" path="$cam_res" id="$cam_id"]\n/m;
        }
        # у WorldEnvironment с этим окружением — camera_attributes сразу после environment
        my $n = ($text =~ s/(^\[node [^\n]*type="WorldEnvironment"[^\n]*\]\n(?:(?!\[)[^\n]*\n)*?environment = ExtResource\("\Q$env_id\E"\)\n)(?:camera_attributes = [^\n]*\n)?/$1camera_attributes = ExtResource("$cam_id")\n/mg);
        next unless $n;
        open(my $o, '>:raw', $scene) or die "$scene: $!";
        print $o $text;
        close $o;
        print "  $scene: WorldEnvironment x$n\n";
    }
}

sub section {
    my ($text, $re) = @_;
    my @blocks = split /(?=^\[)/m, $text;
    for my $b (@blocks) { return $b if $b =~ $re }
    return '';
}

sub props {
    my $b = shift;
    my %p;
    for (split /\n/, $b) {
        next if /^\[/;
        $p{$1} = $2 if /^(\S+) = (.*)$/;
    }
    return %p;
}

# Color( r, g, b, a ) -> (r, g, b, a)
sub color {
    my $v = shift;
    my @c = $v =~ /([-\d.e]+)/g;
    push @c, 1 while @c < 4;
    return @c;
}
sub color4 { my @c = @_; return sprintf('Color(%s, %s, %s, 1)', @c[0..2]) }
sub g4color { my @c = color(shift); return "Color($c[0], $c[1], $c[2], $c[3])" }

sub env_props {
    my ($e, $sky_ref) = @_;
    my @out;
    my $get = sub { my ($k, $def) = @_; exists $e->{$k} ? $e->{$k} : $def };

    push @out, "background_mode = " . $get->('background_mode', 0);
    push @out, "background_color = " . g4color($e->{background_color}) if exists $e->{background_color};
    push @out, "background_energy_multiplier = $e->{background_energy}" if exists $e->{background_energy};
    push @out, "sky = $sky_ref" if $sky_ref;
    push @out, "sky_custom_fov = $e->{background_sky_custom_fov}" if exists $e->{background_sky_custom_fov};
    if (exists $e->{background_sky_orientation}) {
        # Basis(строки) поворота вокруг Y -> угол
        my @m = $e->{background_sky_orientation} =~ /([-\d.e]+)/g;
        push @out, sprintf('sky_rotation = Vector3(0, %.6g, 0)', atan2($m[2], $m[0]));
    }
    push @out, "ambient_light_color = " . g4color($e->{ambient_light_color}) if exists $e->{ambient_light_color};
    if ($get->('background_mode', 0) != 2) {
        # без неба Godot 3 берёт рассеянный свет из цвета с полной энергией;
        # Godot 4 с источником «фон» взял бы чёрный цвет фона
        push @out, "ambient_light_source = 1";
        push @out, "ambient_light_energy = " . $get->('ambient_light_energy', 1.0);
    } else {
        # Godot 3: цвет·E·(1−c) + небо·E·c; Godot 4: цвет·E'·(1−c') + небо·c'
        my $c = $get->('ambient_light_sky_contribution', 1.0);
        my $en = $get->('ambient_light_energy', 1.0);
        my $c4 = $c * $en;
        $c4 = 1.0 if $c4 > 1.0;
        my $e4 = $c4 < 1.0 ? $en * (1 - $c) / (1 - $c4) : $en;
        push @out, sprintf('ambient_light_sky_contribution = %.6g', $c4);
        push @out, sprintf('ambient_light_energy = %.6g', $e4);
    }

    push @out, "tonemap_mode = $e->{tonemap_mode}" if exists $e->{tonemap_mode};
    push @out, "tonemap_exposure = $e->{tonemap_exposure}" if exists $e->{tonemap_exposure};
    push @out, "tonemap_white = $e->{tonemap_white}" if exists $e->{tonemap_white};

    push @out, "ssr_enabled = true" if $get->('ss_reflections_enabled', 'false') eq 'true';

    if ($get->('ssao_enabled', 'false') eq 'true') {
        push @out, "ssao_enabled = true";
        push @out, "ssao_radius = $e->{ssao_radius}" if exists $e->{ssao_radius};
        push @out, "ssao_intensity = " . $get->('ssao_intensity', 1.0);
        push @out, "ssao_power = 1.0";
        push @out, "ssao_light_affect = $e->{ssao_light_affect}" if exists $e->{ssao_light_affect};
        push @out, "ssao_ao_channel_affect = $e->{ssao_ao_channel_affect}" if exists $e->{ssao_ao_channel_affect};
    }

    if ($get->('glow_enabled', 'false') eq 'true') {
        push @out, "glow_enabled = true";
        # уровни Godot 3 по умолчанию — 3 и 5
        for my $l (1 .. 7) {
            my $on = $get->("glow_levels/$l", ($l == 3 || $l == 5) ? 'true' : 'false');
            push @out, "glow_levels/$l = " . ($on eq 'true' ? '1.0' : '0.0');
        }
        push @out, "glow_intensity = " . $get->('glow_intensity', 0.8);
        push @out, "glow_strength = $e->{glow_strength}" if exists $e->{glow_strength};
        push @out, "glow_bloom = $e->{glow_bloom}" if exists $e->{glow_bloom};
        push @out, "glow_blend_mode = " . $get->('glow_blend_mode', 2);
        push @out, "glow_hdr_threshold = $e->{glow_hdr_threshold}" if exists $e->{glow_hdr_threshold};
        push @out, "glow_hdr_scale = $e->{glow_hdr_scale}" if exists $e->{glow_hdr_scale};
    }

    if ($get->('fog_enabled', 'false') eq 'true') {
        my @c = color($get->('fog_color', 'Color( 0.5, 0.6, 0.7, 1 )'));
        push @out, "fog_enabled = true";
        push @out, "fog_mode = 1";
        push @out, "fog_light_color = " . color4(@c);
        push @out, "fog_density = $c[3]";
        push @out, "fog_sky_affect = 0.0";
        my $sun_amount = $get->('fog_sun_amount', 0);
        if ($sun_amount > 0) {
            my @s = color($get->('fog_sun_color', 'Color( 1, 0.9, 0.7, 1 )'));
            my $lum = 0.2126 * $s[0] + 0.7152 * $s[1] + 0.0722 * $s[2];
            push @out, sprintf('fog_sun_scatter = %.4g', $sun_amount * $lum) if $lum > 0;
        }
        push @out, "fog_depth_curve = $e->{fog_depth_curve}" if exists $e->{fog_depth_curve};
        push @out, "fog_depth_begin = " . $get->('fog_depth_begin', 10.0);
        # не задан в Godot 3 — до дальней плоскости камеры
        # 0 в Godot 4 туман выключает — ставим дальность камеры игрока по умолчанию (Settings.distance)
        push @out, "fog_depth_end = " . $get->('fog_depth_end', 600.0);
    }
    return @out;
}

sub sky_props {
    my $s = shift;
    my %def = (
        sky_top_color => 'Color( 0.647059, 0.839216, 0.945098, 1 )',
        sky_horizon_color => 'Color( 0.839216, 0.917647, 0.980392, 1 )',
        sky_curve => 0.09,
        ground_bottom_color => 'Color( 0.156863, 0.184314, 0.211765, 1 )',
        ground_horizon_color => 'Color( 0.423529, 0.396078, 0.372549, 1 )',
        ground_curve => 0.02,
    );
    my $get = sub { exists $s->{$_[0]} ? $s->{$_[0]} : $def{$_[0]} };
    my @out = (
        "sky_top_color = " . g4color($get->('sky_top_color')),
        "sky_horizon_color = " . g4color($get->('sky_horizon_color')),
        "sky_curve = " . $get->('sky_curve'),
    );
    push @out, "sky_energy_multiplier = $s->{sky_energy}" if exists $s->{sky_energy};
    push @out, (
        "ground_bottom_color = " . g4color($get->('ground_bottom_color')),
        "ground_horizon_color = " . g4color($get->('ground_horizon_color')),
        "ground_curve = " . $get->('ground_curve'),
    );
    push @out, "ground_energy_multiplier = $s->{ground_energy}" if exists $s->{ground_energy};
    return @out;
}
