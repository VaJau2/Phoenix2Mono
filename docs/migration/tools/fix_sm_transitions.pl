#!/usr/bin/env perl
# Этап 3 миграции: переходы AnimationNodeStateMachineTransition.
# Godot 3: auto_advance = true / disabled = true; Godot 4.7 эти свойства молча игнорирует,
# вместо них advance_mode (0 — выключен, 1 — включён, 2 — авто).
# Запуск из корня репозитория: perl docs/migration/tools/fix_sm_transitions.pl $(git ls-files '*.tscn' '*.tres')
use strict; use warnings;
my $n = 0;
for my $f (@ARGV) {
    open(my $fh, '<:raw', $f) or die "$f: $!"; my @l = <$fh>; close $fh;
    my ($in, $ch) = (0, 0);
    for (@l) {
        if (/^\[/) { $in = /^\[sub_resource type="AnimationNodeStateMachineTransition"/ || /^\[resource\]/ && $in; next; }
        next unless $in;
        if (s/^auto_advance = true(\r?\n)/advance_mode = 2$1/) { $ch = 1; $n++ }
        elsif (s/^disabled = true(\r?\n)/advance_mode = 0$1/) { $ch = 1; $n++ }
    }
    if ($ch) { open(my $o, '>:raw', $f) or die; print $o @l; close $o; }
}
print "DONE transitions=$n\n";
