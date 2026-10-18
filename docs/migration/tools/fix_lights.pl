#!/usr/bin/env perl
# Этап 5 миграции: затухание Omni/Spot света Godot 3 -> Godot 4.
#
# Godot 3: яркость на расстоянии d = (1 - d/r)^a                  (a — omni/spot_attenuation, r — range)
# Godot 4: яркость на расстоянии d = (1 - (d/r)^4)^2 * d^(-k)      (k — omni/spot_attenuation)
# Множитель d^(-k) зависит от абсолютного расстояния: с теми же значениями фонарь с r = 30 на
# расстоянии 20 м в Godot 4 в ~20 раз темнее. Для каждого источника подбираются k (новое затухание)
# и множитель энергии E так, чтобы кривая Godot 4 повторяла кривую Godot 3 на d от 0.05r до r
# (метод наименьших квадратов). Исходные значения сохраняются в metadata/g3_attenuation и
# metadata/g3_light_energy — по ним повторный запуск источник пропускает.
# Дорожки анимации light_energy этих источников (в том же файле) умножаются на тот же E.
# Всем источникам (включая DirectionalLight3D) без явного light_specular пишется 0.5 — значение
# Godot 3 по умолчанию (в Godot 4 — 1.0: блики вдвое ярче).
#
# Запуск (Godot закрыт): perl docs/migration/tools/fix_lights.pl [--dry] [файлы...]
# без файлов — все .tscn/.tres проекта.
use strict;
use warnings;
use File::Find;

my $dry = grep { $_ eq '--dry' } @ARGV;
my @files = grep { $_ ne '--dry' } @ARGV;
if (!@files) {
    find(sub { push @files, $File::Find::name if /\.(tscn|tres)$/ && $File::Find::name !~ m{/\.godot/} }, '.');
}

my ($nlights, $nfiles) = (0, 0);
for my $file (sort @files) {
    open(my $in, '<:raw', $file) or die "$file: $!";
    my $text = do { local $/; <$in> };
    close $in;
    next unless $text =~ /type="(OmniLight3D|SpotLight3D|DirectionalLight3D)"/;

    my @blocks = split /(?=^\[)/m, $text;
    my %scale;   # путь узла -> E
    my $changed = 0;
    for my $b (@blocks) {
        if ($b =~ /^\[node [^
]*type="(OmniLight3D|SpotLight3D|DirectionalLight3D)"/ && $b !~ /^light_specular = /m) {
            $b =~ s/
*\z//;
            $b .= "
light_specular = 0.5

";
            $changed++;
        }
    }
    for my $b (@blocks) {
        next unless $b =~ /^\[node name="([^"]+)" type="(OmniLight3D|SpotLight3D)"(.*)\]/;
        my ($name, $type, $rest) = ($1, $2, $3);
        next if $b =~ /^metadata\/g3_attenuation/m;
        my $p = $type eq 'OmniLight3D' ? 'omni' : 'spot';
        my $range = $b =~ /^${p}_range = ([-\d.e]+)/m ? $1 : 5.0;
        my $att = $b =~ /^${p}_attenuation = ([-\d.e]+)/m ? $1 : 1.0;
        my $energy = $b =~ /^light_energy = ([-\d.e]+)/m ? $1 : 1.0;
        my ($k, $e, $err) = fit($range, $att);

        my $new_energy = sprintf('%.4g', $energy * $e);
        my $new_k = sprintf('%.3g', $k);
        $b =~ s/^${p}_attenuation = .*\n//m;
        $b =~ s/^light_energy = .*\n//m;
        # свойства — перед пустой строкой в конце блока
        $b =~ s/\n*\z//;
        $b .= "\nlight_energy = $new_energy\n${p}_attenuation = $new_k\nmetadata/g3_attenuation = $att\nmetadata/g3_light_energy = $energy\n\n";

        my $parent = $rest =~ /parent="([^"]*)"/ ? $1 : undef;
        my $path = !defined $parent ? '.' : $parent eq '.' ? $name : "$parent/$name";
        $scale{$path} = $e;
        printf "%s  %-40s r=%-6g a=%-6g -> k=%-6s E=%-7.3f energy %g -> %s  (ошибка %.0f%%)\n",
            $file, $path, $range, $att, $new_k, $e, $energy, $new_energy, $err * 100;
        $nlights++;
    }
    next unless %scale || $changed;

    # дорожки анимации light_energy (пути относительно корня сцены — AnimationPlayer лежит в корне)
    for my $b (@blocks) {
        next unless $b =~ /^\[sub_resource type="Animation"/;
        my @tracks = $b =~ /^tracks\/(\d+)\/path = NodePath\("([^":]+):light_energy"\)/mg;
        while (my ($t, $node) = splice(@tracks, 0, 2)) {
            next unless exists $scale{$node};
            my $e = $scale{$node};
            $b =~ s{(^tracks/$t/keys = \{.*?"values": \[)([^\]]*)(\])}{$1 . join(', ', map { sprintf('%.4g', $_ * $e) } split /,\s*/, $2) . $3}mse;
            print "$file  дорожка $node:light_energy x $e\n";
        }
    }

    $nfiles++;
    next if $dry;
    open(my $out, '>:raw', $file) or die "$file: $!";
    print $out join('', @blocks);
    close $out;
}
print "источников: $nlights, файлов: $nfiles" . ($dry ? " (--dry, файлы не изменены)" : '') . "\n";

# k и E: E*(1-(d/r)^4)^2*d^-k ~ (1-d/r)^a на d из [0.05r, r]
my %fit_cache;
sub fit {
    my ($r, $a) = @_;
    $fit_cache{"$r $a"} //= [fit_uncached($r, $a)];
    return @{ $fit_cache{"$r $a"} };
}

sub fit_uncached {
    my ($r, $a) = @_;
    my @d = map { $r * (0.05 + 0.95 * $_ / 200) } 0 .. 200;
    my @t = map { (1 - $_ / $r) ** $a } @d;
    my $tt = 0; $tt += $_ ** 2 for @t;
    my ($best_k, $best_e, $best_err) = (0, 1, 1e99);
    for (my $k = 0; $k <= 4.0001; $k += 0.01) {
        my ($gt, $gg) = (0, 0);
        my @g;
        for my $i (0 .. $#d) {
            my $x = $d[$i] / $r;
            my $w = (1 - $x ** 4); $w = 0 if $w < 0; $w *= $w;
            $g[$i] = $w * $d[$i] ** (-$k);
            $gt += $g[$i] * $t[$i];
            $gg += $g[$i] ** 2;
        }
        next unless $gg > 0;
        my $e = $gt / $gg;
        my $err = 0;
        $err += ($e * $g[$_] - $t[$_]) ** 2 for 0 .. $#d;
        ($best_k, $best_e, $best_err) = ($k, $e, $err) if $err < $best_err;
    }
    return ($best_k, $best_e, sqrt($best_err / $tt));
}
