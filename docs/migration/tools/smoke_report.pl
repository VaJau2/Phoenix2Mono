#!/usr/bin/env perl
# Сводка лога smoke_test.gd: ошибки и исключения, сгруппированные по уровню и месту в коде проекта.
# Запуск: perl docs/migration/tools/smoke_report.pl smoke.log [--warnings]
# Для исключений C# место — первая строка стека из assets/scripts, для ошибок движка — первая
# строка "at:"/C# backtrace из проекта (если есть), иначе исходник движка.
use strict;
use warnings;

my $log = shift or die "usage: $0 smoke.log [--warnings]\n";
my $with_warnings = grep { $_ eq '--warnings' } @ARGV;

open(my $fh, '<:raw', $log) or die "$log: $!";
my @lines = map { s/\e\[[0-9;]*m//gr } <$fh>;
close $fh;

my $level = 'MAIN';
my (%count, %first, @order);
for (my $i = 0; $i < @lines; $i++) {
    my $l = $lines[$i];
    if ($l =~ /^SMOKE LEVEL (\d+) start/) { $level = $1; next }
    if ($l =~ /^SMOKE ALL DONE/) { $level = 'EXIT'; next }
    next unless $l =~ /^(ERROR|SCRIPT ERROR|WARNING): (.*)/;
    my ($kind, $msg) = ($1, $2);
    next if $kind eq 'WARNING' && !$with_warnings;
    next if $level eq 'EXIT';   # утечки при выходе
    # место: первая строка проекта в следующих строках стека
    my $where = '';
    for my $j ($i + 1 .. $i + 40) {
        last if $j > $#lines || $lines[$j] =~ /^(ERROR|SCRIPT ERROR|WARNING|SMOKE)/;
        if ($lines[$j] =~ m{assets[\\/]scripts[\\/](\S+?\.cs):line (\d+)} || $lines[$j] =~ m{\((?:D:)?[^()]*assets[\\/]scripts[\\/]([^():]+\.cs):(\d+)\)}) { $where = "$1:$2"; last }
        if (!$where && $lines[$j] =~ /^\s+at: (.*)/) { $where = $1 }
    }
    $msg =~ s/#\d+/#N/g;
    $msg =~ s/\d+(\.\d+)?/N/g if $msg =~ /out of bounds|Index /;
    my $key = "$kind\t$msg\t$where";
    push @order, $key unless $count{$key};
    $count{$key}{$level}++;
}

for my $key (sort { total($count{$b}) <=> total($count{$a}) } @order) {
    my ($kind, $msg, $where) = split /\t/, $key;
    my $levels = join ', ', map { "$_" . ($count{$key}{$_} > 1 ? "×$count{$key}{$_}" : '') } sort { $a cmp $b } keys %{ $count{$key} };
    printf "%5d  %-13s %s\n       где: %s\n       уровни: %s\n", total($count{$key}), $kind, substr($msg, 0, 200), $where || '?', $levels;
}

sub total { my $h = shift; my $s = 0; $s += $_ for values %$h; $s }
