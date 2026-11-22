#!/usr/bin/env perl
# B-43: значения по умолчанию у примитивных мешей и форм (этап 4 миграции).
#
# В сценах хранятся только свойства, отличные от значения по умолчанию. В Godot 4 умолчания
# у части примитивов стали вдвое меньше, и ресурсы, которые в Godot 3 полагались на умолчание,
# молча уменьшились (щит единорога, кнопки на BellgatesBase):
#
#   Godot 3                                   Godot 4
#   CubeMesh.size = (2, 2, 2)                 BoxMesh.size = (1, 1, 1)
#   PrismMesh.size = (2, 2, 2)                PrismMesh.size = (1, 1, 1)
#   SphereMesh.radius = 1, height = 2         radius = 0.5, height = 1
#   CylinderMesh.top/bottom_radius = 1        top/bottom_radius = 0.5 (height = 2 в обеих)
#   CapsuleMesh.radius = 1                    radius = 0.5 (height пересчитан fix_capsules.pl)
#   BoxShape.extents = (1, 1, 1)              BoxShape3D.size = (1, 1, 1) — полуразмер 0.5
#   SphereShape.radius = 1                    SphereShape3D.radius = 0.5
#   CylinderShape.radius = 1                  CylinderShape3D.radius = 0.5 (height = 2 в обеих)
#   CapsuleShape.radius = 1                   CapsuleShape3D.radius = 0.5
#   (PlaneMesh 2×2 и QuadMesh 1×1 — одинаковые, их не трогаем.)
#
# Что делает: для каждого такого ресурса сцены ищет ресурс с тем же id в версии файла Godot 3
# (родитель коммита автоконвертации, $G3_REF) и, если там свойство не было задано, а в Godot 4
# его тоже нет, дописывает значение по умолчанию Godot 3. Ресурсы, созданные уже в Godot 4
# (нет в $G3_REF), не трогает.
# Идемпотентен: уже записанные свойства не меняет.
#
# Запуск из корня репозитория: perl docs/migration/tools/fix_primitive_defaults.pl [--dry] [файлы...]

use strict;
use warnings;

# Последнее состояние проекта на Godot 3 — родитель коммита автоконвертации
my $G3_REF = q{a11f4b5b^};

my $dry = 0;
my @files = grep { $_ eq '--dry' ? (($dry = 1), 0) : 1 } @ARGV;
if (!@files) {
    open(my $git, '-|', 'git', 'ls-files', '-z', '*.tscn', '*.tres') or die $!;
    local $/ = "\0";
    while (my $f = <$git>) { chomp $f; push @files, $f unless $f =~ m{^docs/}; }
    close $git;
}

# Тип Godot 4 => [тип Godot 3, [свойство Godot 3, строка Godot 4 для записи], ...]
my %rules = (
    BoxMesh         => ['CubeMesh',      ['size',        'size = Vector3(2, 2, 2)']],
    PrismMesh       => ['PrismMesh',     ['size',        'size = Vector3(2, 2, 2)']],
    SphereMesh      => ['SphereMesh',    ['radius',      'radius = 1.0'], ['height', 'height = 2.0']],
    CylinderMesh    => ['CylinderMesh',  ['top_radius',  'top_radius = 1.0'], ['bottom_radius', 'bottom_radius = 1.0']],
    CapsuleMesh     => ['CapsuleMesh',   ['radius',      'radius = 1.0']],
    BoxShape3D      => ['BoxShape',      ['extents',     'size = Vector3(2, 2, 2)']],
    SphereShape3D   => ['SphereShape',   ['radius',      'radius = 1.0']],
    CylinderShape3D => ['CylinderShape', ['radius',      'radius = 1.0']],
    CapsuleShape3D  => ['CapsuleShape',  ['radius',      'radius = 1.0']],
);
# Свойство Godot 4, которое соответствует свойству Godot 3 (если имя другое)
my %g4name = (extents => 'size');

# id => {type, props{}} для ресурсов-примитивов файла
sub parse {
    my ($text) = @_;
    my (%res, $cur);
    for my $line (split /\n/, $text) {
        if ($line =~ /^\[/) {
            $cur = undef;
            if ($line =~ /^\[(sub_resource|gd_resource) type="(\w+)"/) {
                my ($kind, $type) = ($1, $2);
                my $id = $kind eq 'gd_resource' ? '' : ($line =~ /\bid="?([^"\s\]]+)"?/ ? $1 : next);
                $cur = $res{$id} = { type => $type, props => {} };
            }
            next;
        }
        $cur->{props}{$1} = 1 if $cur && $line =~ /^(\w+)\s*=/;
    }
    return \%res;
}

my ($changed_files, $added, $skipped_new) = (0, 0, 0);
for my $file (@files) {
    open(my $fh, '<:raw', $file) or die "$file: $!";
    my $text = do { local $/; <$fh> };
    close $fh;
    next unless $text =~ /type="(?:${\ join '|', keys %rules})"/;

    my $g3text = do {
        open(my $git, '-|', 'git', 'show', "$G3_REF:$file") or die $!;
        local $/; my $t = <$git> // ''; close $git; $t;
    };
    if ($? != 0 || $g3text eq '') { print "SKIP $file: нет в $G3_REF\n"; next; }
    my $g3 = parse($g3text);
    my $g4 = parse($text);

    my %insert;   # id => [строки]
    for my $id (sort keys %$g4) {
        my $r = $g4->{$id};
        my $rule = $rules{$r->{type}} or next;
        my ($g3type, @props) = @$rule;
        my $old = $g3->{$id};
        if (!$old || $old->{type} ne $g3type) { $skipped_new++; next; }
        for my $p (@props) {
            my ($g3prop, $line) = @$p;
            next if $old->{props}{$g3prop};                          # в Godot 3 было задано явно
            next if $r->{props}{$g4name{$g3prop} // $g3prop};        # уже записано
            next if $g3prop eq 'extents' && $r->{props}{extents};    # extents читается совместимостью
            push @{ $insert{$id} }, $line;
            print "$file [$r->{type} $id] + $line\n";
            $added++;
        }
    }
    next unless %insert;

    # Дописать свойства сразу после заголовка ресурса
    my @out;
    for my $line (split /(?<=\n)/, $text) {
        push @out, $line;
        if ($line =~ /^\[(sub_resource|gd_resource) type="(\w+)"/) {
            my $id = $1 eq 'gd_resource' ? '' : ($line =~ /\bid="?([^"\s\]]+)"?/ ? $1 : undef);
            if (defined $id && $insert{$id}) {
                # gd_resource: свойства идут после строки [resource]
                if ($1 eq 'gd_resource') { $insert{"\0res"} = delete $insert{$id}; next; }
                push @out, map { "$_\n" } @{ delete $insert{$id} };
            }
        }
        if ($line =~ /^\[resource\]/ && $insert{"\0res"}) {
            push @out, map { "$_\n" } @{ delete $insert{"\0res"} };
        }
    }
    die "$file: не все свойства вставлены\n" if %insert;
    $changed_files++;
    next if $dry;
    open(my $oh, '>:raw', $file) or die "$file: $!";
    print $oh join '', @out;
    close $oh;
}
print "\nФайлов: $changed_files, свойств дописано: $added, ресурсов Godot 4 (пропущено): $skipped_new" . ($dry ? " (--dry, файлы не менялись)" : "") . "\n";
