#!/usr/bin/env perl
# Этап 3 миграции: капсулы Godot 3 -> Godot 4 (конвертер Godot их не пересчитывает).
#
# Godot 3: CapsuleShape.height / CapsuleMesh.mid_height — высота только цилиндрической части,
#          капсула вытянута вдоль оси Z.
# Godot 4: CapsuleShape3D.height / CapsuleMesh.height — полная высота (с полусферами),
#          капсула вытянута вдоль оси Y.
#
# Что делает:
#   1. height = height + 2 * radius у всех CapsuleShape3D и CapsuleMesh (значения по умолчанию
#      Godot 3: radius = 1, height/mid_height = 1);
#   2. узлы, которые используют капсулу (shape = ... у CollisionShape3D, mesh = ... у MeshInstance3D),
#      поворачиваются на +90° вокруг локальной X, чтобы новая ось Y совпала со старой Z.
# Остальные ссылки на капсулы (например, меш частиц draw_pass) только выводятся в WARN.
#
# НЕ идемпотентен — запускать один раз, сразу после конвертации проекта.
# Запуск из корня репозитория: perl docs/migration/tools/fix_capsules.pl [файлы...]

use strict;
use warnings;

my @files = @ARGV;
if (!@files) {
    open(my $git, '-|', 'git', 'ls-files', '-z', '*.tscn', '*.tres') or die $!;
    local $/ = "\0";
    while (my $f = <$git>) { chomp $f; push @files, $f; }
    close $git;
}

my %stat = (resources => 0, nodes => 0, inserted_transforms => 0, warn => 0);

for my $file (@files) {
    open(my $fh, '<:raw', $file) or die "$file: $!";
    my @lines = <$fh>;
    close $fh;

    my $text = join '', @lines;
    next unless $text =~ /type="Capsule(?:Shape3D|Mesh)"/;

    my %caps;   # id -> 'shape' | 'mesh'
    my $changed = 0;

    # 1. высота ресурсов
    my @out;
    for (my $i = 0; $i < @lines; $i++) {
        my $line = $lines[$i];
        if ($line =~ /^\[sub_resource type="Capsule(Shape3D|Mesh)" id=(\S+?)\]/) {
            my ($kind, $id) = ($1 eq 'Shape3D' ? 'shape' : 'mesh', $2);
            $caps{$id} = $kind;
            # блок ресурса — до пустой строки или следующего заголовка
            my @block = ($line);
            my $j = $i + 1;
            while ($j < @lines && $lines[$j] !~ /^\[/ && $lines[$j] !~ /^\r?\n$/) { push @block, $lines[$j]; $j++; }
            my ($radius) = map { /^radius = ([-\d.e]+)/ ? $1 : () } @block;
            $radius //= 1;
            my $has_height = grep { /^height = / } @block;
            if ($has_height) {
                s/^height = ([-\d.e]+)/"height = " . num($1 + 2 * $radius)/e for @block;
            } else {
                push @block, "height = " . num(1 + 2 * $radius) . "\n";
            }
            push @out, @block;
            $i = $j - 1;
            $stat{resources}++;
            $changed = 1;
            next;
        }
        push @out, $line;
    }
    @lines = @out;

    # 2. узлы, использующие капсулы
    @out = ();
    my @node;   # строки текущего узла
    my $flush = sub {
        return unless @node;
        my $hdr = $node[0];
        my $uses = grep { /^(shape|mesh) = SubResource\( (\S+?) \)/ && $caps{$2} && ($1 eq $caps{$2}) } @node;
        if ($uses) {
            my $tr = 0;
            for (@node) {
                if (/^transform = Transform3D\(([^)]*)\)/) {
                    my @m = split /\s*,\s*/, $1 =~ s/^\s+|\s+$//gr;
                    # строки базиса: B' = B * Rx(+90): col1' = col2, col2' = -col1
                    for my $r (0 .. 2) {
                        my ($c1, $c2) = ($m[$r * 3 + 1], $m[$r * 3 + 2]);
                        $m[$r * 3 + 1] = $c2;
                        $m[$r * 3 + 2] = neg($c1);
                    }
                    $_ = "transform = Transform3D( " . join(', ', @m) . " )\n";
                    $tr = 1;
                }
            }
            if (!$tr) {
                if ($hdr =~ / type="/) {
                    splice @node, 1, 0, "transform = Transform3D( 1, 0, 0, 0, 0, -1, 0, 1, 0, 0, 0, 0 )\n";
                    $stat{inserted_transforms}++;
                } else {
                    print "WARN $file: узел-переопределение без transform, не повёрнут: $hdr";
                    $stat{warn}++;
                }
            }
            $stat{nodes}++;
        }
        push @out, @node;
        @node = ();
    };
    for my $line (@lines) {
        if ($line =~ /^\[/) {
            $flush->();
            if ($line =~ /^\[node /) { @node = ($line); next; }
            push @out, $line;
            next;
        }
        if (@node) { push @node, $line } else { push @out, $line }
    }
    $flush->();

    # ссылки на капсулы не из shape/mesh узлов
    for my $id (keys %caps) {
        for (grep { /SubResource\( \Q$id\E \)/ && !/^(shape|mesh) = / } @out) {
            print "WARN $file: капсула $id используется не узлом, ось не исправлена: $_";
            $stat{warn}++;
        }
    }

    if ($changed) {
        open(my $o, '>:raw', $file) or die "$file: $!";
        print $o @out;
        close $o;
    }
}

print "DONE ", join(' ', map { "$_=$stat{$_}" } sort keys %stat), "\n";

sub num { my $v = sprintf('%.6g', $_[0]); return $v }
sub neg { my $v = $_[0]; return $v =~ /^-/ ? substr($v, 1) : ($v == 0 ? $v : "-$v") }
