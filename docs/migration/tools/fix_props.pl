#!/usr/bin/env perl
# Этап 3 миграции: свойства узлов Godot 3, которые Godot 4.7 при загрузке молча игнорирует
# (проверено тестовой сценой, см. docs/migration/plan.md, этап 3):
#   material/N (MeshInstance)        -> surface_material_override/N
#   Button/LineEdit: align           -> alignment (у Label align понимается и остаётся)
#   Label: autowrap = true           -> autowrap_mode = 3 (по словам)
#   ProgressBar: percent_visible     -> show_percentage
#   TextureRect: stretch_mode N      -> N - 1 (в Godot 3 нулевым был отдельный SCALE_ON_EXPAND)
# Для узлов-переопределений без type (наследуемые сцены) material/N и stretch_mode тоже
# переводятся: в проекте они встречаются только у MeshInstance3D / TextureRect.
#
# НЕ идемпотентен из-за stretch_mode — запускать один раз.
# Запуск из корня репозитория: perl docs/migration/tools/fix_props.pl
use strict;
use warnings;

my @files = @ARGV;
if (!@files) {
    open(my $git, '-|', 'git', 'ls-files', '-z', '*.tscn') or die $!;
    local $/ = "\0";
    while (my $f = <$git>) { chomp $f; push @files, $f; }
}

my %stat;
for my $f (@files) {
    open(my $fh, '<:raw', $f) or die "$f: $!";
    my @l = <$fh>;
    close $fh;

    my ($type, $in_node, $changed) = ('', 0, 0);
    for (@l) {
        if (/^\[/) {
            $in_node = /^\[node /;
            $type = $in_node ? (/ type="(\w+)"/ ? $1 : '') : '';
            next;
        }
        next unless $in_node;
        my $before = $_;

        s/^material\/(\d+) = /surface_material_override\/$1 = / and $stat{material}++;
        if ($type eq 'Button' || $type eq 'LineEdit') {
            s/^align = /alignment = / and $stat{alignment}++;
        }
        if ($type eq 'Label') {
            s/^autowrap = true/autowrap_mode = 3/ and $stat{autowrap}++;
        }
        if ($type eq 'ProgressBar') {
            s/^percent_visible = /show_percentage = / and $stat{show_percentage}++;
        }
        if ($type eq 'TextureRect' || $type eq '') {
            s/^stretch_mode = (\d+)/"stretch_mode = " . ($1 > 0 ? $1 - 1 : 0)/e and $stat{stretch_mode}++;
        }
        $changed = 1 if $_ ne $before;
    }

    if ($changed) {
        open(my $o, '>:raw', $f) or die "$f: $!";
        print $o @l;
        close $o;
    }
}
print "DONE ", join(' ', map { "$_=$stat{$_}" } sort keys %stat), "\n";
