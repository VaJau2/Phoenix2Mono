#!/usr/bin/env perl
# Ставит суставам PhysicalBone3D значения softness/relaxation по умолчанию Jolt.
# Jolt эти параметры не поддерживает и при создании каждого сустава с другим значением пишет
# "Hinge joint softness/relaxation ..." / "Cone twist joint relaxation ... is not supported
# when using Jolt Physics" (десятки предупреждений на каждый уровень). Поведение не меняется:
# Jolt значения всё равно игнорирует. Исходные значения из Godot 3 — в истории git.
#   hinge (joint_type = 3):      angular_limit_softness = 0.9, angular_limit_relaxation = 1.0
#   cone twist (joint_type = 2): softness = 0.8, relaxation = 1.0
# Запуск: perl docs/migration/tools/fix_jolt_joints.pl [--dry-run] <файлы .tscn>
#   (все сцены: grep -rlE "joint_constraints/" --include=*.tscn objects scenes | xargs perl ...)
use strict;
use warnings;

my $dry = grep { $_ eq '--dry-run' } @ARGV;
my %defaults = (
    3 => { 'angular_limit_softness' => '0.9', 'angular_limit_relaxation' => '1.0' },
    2 => { 'softness' => '0.8', 'relaxation' => '1.0' },
);

for my $file (grep { $_ ne '--dry-run' } @ARGV) {
    open my $fh, '<:raw', $file or die "$file: $!";
    my @lines = <$fh>;
    close $fh;

    my ($type, $changed) = ('', 0);
    for (@lines) {
        $type = '' if /^\[/;
        $type = $1 if /^joint_type = (\d+)/;
        next unless /^joint_constraints\/(\w+) = (\S+)$/;
        my ($param, $value) = ($1, $2);
        my $def = $defaults{$type} && $defaults{$type}{$param};
        next if !defined $def || $value == $def;
        $_ = "joint_constraints/$param = $def\n";
        $changed++;
    }
    print "$file: $changed\n";
    next if $dry || !$changed;
    open my $oh, '>:raw', $file or die "$file: $!";
    print $oh @lines;
    close $oh;
}
