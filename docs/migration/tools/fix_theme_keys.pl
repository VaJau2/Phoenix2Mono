#!/usr/bin/env perl
# Этап 4 миграции: ключи тем (Theme) Godot 3 -> Godot 4 (B-30).
#
# Конвертер переименовал переопределения на узлах (custom_colors/... -> theme_override_colors/...), но не ключи
# внутри ресурсов Theme (.tres и встроенные в сцены). Godot 4 неизвестные ключи молча игнорирует: у кнопок не
# менялся цвет текста при наведении/нажатии, пропали цвета теней текста, выделения и курсора в полях ввода и т. п.
#
# Переименования (тип/вид/имя): см. %rename ниже. Дополнительно:
# - Button: в Godot 4 есть состояния, которых не было в Godot 3 — font_hover_pressed_color (наведение на
#   нажатую кнопку) = цвет нажатия, font_focus_color (кнопка в фокусе) = обычный цвет, как в Godot 3;
# - Label/RichTextLabel: shadow_as_outline удалён; в Godot 4 у тени по умолчанию обводка 1 px
#   (shadow_outline_size = 1) — пишется 0 (простая тень, как в Godot 3);
# - ProgressBar/colors/font_color_shadow: в Godot 4 у ProgressBar нет тени текста — удаляется.
# Запуск: perl docs/migration/tools/fix_theme_keys.pl [файлы...]  (без файлов — все .tres/.tscn проекта)
use strict;
use warnings;
use File::Find;

my %rename = (
    'Button/colors/font_color_pressed'          => 'Button/colors/font_pressed_color',
    'Button/colors/font_color_hover'            => 'Button/colors/font_hover_color',
    'Button/colors/font_color_disabled'         => 'Button/colors/font_disabled_color',
    'Button/constants/hseparation'              => 'Button/constants/h_separation',
    'Label/colors/font_color_shadow'            => 'Label/colors/font_shadow_color',
    'Label/colors/font_outline_modulate'        => 'Label/colors/font_outline_color',
    'RichTextLabel/colors/font_color_shadow'    => 'RichTextLabel/colors/font_shadow_color',
    'RichTextLabel/colors/font_color_selected'  => 'RichTextLabel/colors/font_selected_color',
    'RichTextLabel/constants/table_hseparation' => 'RichTextLabel/constants/table_h_separation',
    'RichTextLabel/constants/table_vseparation' => 'RichTextLabel/constants/table_v_separation',
    'LineEdit/colors/font_color_uneditable'     => 'LineEdit/colors/font_uneditable_color',
    'LineEdit/colors/font_color_selected'       => 'LineEdit/colors/font_selected_color',
    'LineEdit/colors/cursor_color'              => 'LineEdit/colors/caret_color',
    'LineEdit/constants/minimum_spaces'         => 'LineEdit/constants/minimum_character_width',
);

my @files = @ARGV;
if (!@files) {
    find(sub {
        if ($_ eq '.godot') { $File::Find::prune = 1; return }
        push @files, $File::Find::name if /\.(tres|tscn)$/;
    }, '.');
}

my $total = 0;
for my $file (@files) {
    open(my $in, '<:raw', $file) or die "$file: $!";
    my $text = do { local $/; <$in> };
    close $in;
    next unless $text =~ m{^(Button|Label|RichTextLabel|LineEdit|ProgressBar)/(colors|constants)/}m;

    my $n = 0;
    for my $old (keys %rename) {
        $n += ($text =~ s{^\Q$old\E = }{$rename{$old} = }mg);
    }
    $n += ($text =~ s{^(Label|RichTextLabel)/constants/shadow_as_outline = \d+\n}{$1/constants/shadow_outline_size = 0\n}mg);
    $n += ($text =~ s{^ProgressBar/colors/font_color_shadow = [^\n]*\n}{}mg);
    # новые состояния кнопки — по цветам Godot 3
    $text =~ s{^(Button/colors/font_pressed_color = ([^\n]*)\n)(?![\s\S]*?^Button/colors/font_hover_pressed_color)}{$1Button/colors/font_hover_pressed_color = $2\n}m and $n++;
    $text =~ s{^(Button/colors/font_color = ([^\n]*)\n)(?![\s\S]*?^Button/colors/font_focus_color)}{$1Button/colors/font_focus_color = $2\n}m and $n++;
    next unless $n;

    open(my $out, '>:raw', $file) or die "$file: $!";
    print $out $text;
    close $out;
    print "$file: $n\n";
    $total += $n;
}
print "изменений: $total\n";
