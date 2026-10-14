#!/usr/bin/env perl
# Этап 1.3 миграции: после `--convert-3to4` вернуть исходные имена узлов в .tscn/.tres.
#
# Конвертер Godot 4 переименовывает слова целиком, поэтому узел "Skeleton" становится "Skeleton3D",
# "CollisionShape" — "CollisionShape3D" и т. д. Имена узлов — это данные (на них ссылаются сохранения
# и C# код), поэтому их возвращаем, а типы и имена свойств оставляем сконвертированными.
#
# Конвертер не меняет количество строк, поэтому строки оригинала и результата сопоставляются 1:1:
#   [node ...]        — атрибуты name, parent, groups берутся из оригинала;
#   [connection ...]  — атрибуты from, to берутся из оригинала (signal и method — из результата);
#   [editable ...]    — строка целиком из оригинала;
#   NodePath("a/b:c") — в любой строке: путь к узлу ("a/b") из оригинала, свойство (":c") из результата.
#
# Запуск из корня репозитория:
#   perl docs/migration/tools/restore_node_names.pl <ревизия до конвертации> [файлы...]
# Без списка файлов обрабатываются все .tscn/.tres, изменённые между ревизией и рабочей копией.

use strict;
use warnings;

my $rev = shift @ARGV or die "usage: $0 <revision> [files...]\n";
my @files = @ARGV;
if (!@files) {
    open(my $git, '-|', 'git', 'diff', '--name-only', '-z', $rev, '--', '*.tscn', '*.tres') or die $!;
    local $/ = "\0";
    while (my $f = <$git>) { chomp $f; push @files, $f; }
    close $git;
}

my ($changed_files, $changed_lines, $skipped) = (0, 0, 0);

for my $file (@files) {
    next unless -f $file;
    my @orig = read_git($rev, $file);
    next unless @orig;
    my @conv = read_file($file);

    if (@orig != @conv) {
        print "SKIP $file: строк было ", scalar(@orig), ", стало ", scalar(@conv), "\n";
        $skipped++;
        next;
    }

    my $file_changed = 0;
    for my $i (0 .. $#conv) {
        next if $conv[$i] eq $orig[$i];
        my $fixed = restore_line($orig[$i], $conv[$i], "$file:" . ($i + 1));
        if ($fixed ne $conv[$i]) {
            $conv[$i] = $fixed;
            $file_changed = 1;
            $changed_lines++;
        }
    }

    if ($file_changed) {
        write_file($file, @conv);
        $changed_files++;
    }
}

print "DONE files=$changed_files lines=$changed_lines skipped=$skipped\n";

sub restore_line {
    my ($orig, $conv, $where) = @_;

    if ($conv =~ /^\[editable /) {
        return $orig;
    }
    if ($conv =~ /^\[node /) {
        $conv = copy_attr($orig, $conv, $_) for qw(name parent);
        $conv = copy_attr_raw($orig, $conv, 'groups', qr/\[[^\]]*\]/);
    }
    elsif ($conv =~ /^\[connection /) {
        $conv = copy_attr($orig, $conv, $_) for qw(from to);
    }

    $conv = restore_node_paths($orig, $conv, $where);

    # Значения свойств вида key = ... (кроме исходников встроенных скриптов и шейдеров)
    if ($conv =~ /^([A-Za-z_][\w\/]*) = / && $1 ne 'script/source' && $1 ne 'code') {
        $conv = restore_strings($orig, $conv, $1);
    }
    return $conv;
}

# Строки в значениях свойств: пути к узлам в экспортированных string-переменных
# ("Armature/Skeleton/Body"), фильтры AnimationTree ("Armature/Skeleton:head"), resource_name.
# У пути возвращается узловая часть, часть после ":" (свойство/кость) остаётся из результата.
sub restore_strings {
    my ($orig, $conv, $key) = @_;
    my @orig_strs = $orig =~ /"((?:[^"\\]|\\.)*)"/g;
    my @conv_strs = $conv =~ /"((?:[^"\\]|\\.)*)"/g;
    return $conv if @orig_strs != @conv_strs;

    my $i = 0;
    $conv =~ s{"((?:[^"\\]|\\.)*)"}{
        my ($o, $c) = ($orig_strs[$i++], $1);
        if ($o ne $c && $key eq 'resource_name') {
            $c = $o;
        }
        elsif ($o ne $c && $o =~ m{[/:]} && $o !~ m{^res://}) {
            my ($orig_node) = split_path($o);
            my (undef, $conv_sub) = split_path($c);
            $c = $orig_node . $conv_sub;
        }
        '"' . $c . '"';
    }ge;
    return $conv;
}

# Атрибут вида name="..."
sub copy_attr {
    my ($orig, $conv, $attr) = @_;
    my ($value) = $orig =~ /\b\Q$attr\E="((?:[^"\\]|\\.)*)"/;
    return $conv unless defined $value;
    $conv =~ s/\b\Q$attr\E="(?:[^"\\]|\\.)*"/$attr="$value"/;
    return $conv;
}

# Атрибут с произвольным значением, например groups=["a", "b"]
sub copy_attr_raw {
    my ($orig, $conv, $attr, $value_re) = @_;
    my ($value) = $orig =~ /\b\Q$attr\E=($value_re)/;
    return $conv unless defined $value;
    $conv =~ s/\b\Q$attr\E=$value_re/$attr=$value/;
    return $conv;
}

sub restore_node_paths {
    my ($orig, $conv, $where) = @_;
    my @orig_paths = $orig =~ /NodePath\("((?:[^"\\]|\\.)*)"\)/g;
    return $conv unless @orig_paths;

    my @conv_paths = $conv =~ /NodePath\("((?:[^"\\]|\\.)*)"\)/g;
    if (@orig_paths != @conv_paths) {
        print "WARN $where: NodePath было ", scalar(@orig_paths), ", стало ", scalar(@conv_paths), " — строка не тронута\n";
        return $conv;
    }

    my $i = 0;
    $conv =~ s{NodePath\("((?:[^"\\]|\\.)*)"\)}{
        my ($orig_node) = split_path($orig_paths[$i]);
        my (undef, $conv_sub) = split_path($1);
        $i++;
        'NodePath("' . $orig_node . $conv_sub . '")';
    }ge;
    return $conv;
}

# "a/b:prop:sub" -> ("a/b", ":prop:sub")
sub split_path {
    my ($path) = @_;
    my $pos = index($path, ':');
    return $pos < 0 ? ($path, '') : (substr($path, 0, $pos), substr($path, $pos));
}

sub read_git {
    my ($rev, $file) = @_;
    open(my $fh, '-|', 'git', 'show', "$rev:$file") or return ();
    binmode $fh;
    my @lines = <$fh>;
    close $fh;
    return $? == 0 ? @lines : ();
}

sub read_file {
    my ($file) = @_;
    open(my $fh, '<:raw', $file) or die "$file: $!";
    my @lines = <$fh>;
    close $fh;
    return @lines;
}

sub write_file {
    my ($file, @lines) = @_;
    open(my $fh, '>:raw', $file) or die "$file: $!";
    print $fh @lines;
    close $fh;
}
