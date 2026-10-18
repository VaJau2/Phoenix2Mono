#!/usr/bin/env perl
# Проверка экземпляров сцен интерфейса после пересохранения редактором Godot 4 (B-7, B-26).
# Редактор при «Обновлении файлов проекта» дописал экземплярам `anchors_preset = 0` / `layout_mode = 0`:
# привязки корня вложенной сцены (например, к центру) сбрасываются на левый верхний угол, и элемент
# уезжает за экран (меню загрузки, таблица сохранений). Скрипт печатает экземпляры с `layout_mode = 0`,
# которые теряют ненулевые привязки корня своей сцены. Исправление: убрать `layout_mode = 0` у экземпляра
# (и при желании явно записать нужные anchor_*).
# Запуск: perl docs/migration/tools/check_layout.pl
use strict;
use warnings;
use File::Find;

my @files; find(sub { if ($_ eq '.godot') { $File::Find::prune = 1; return } push @files, $File::Find::name if /\.tscn$/ }, '.');
my %root;   # res path -> anchors of root
for my $f (@files) {
  open(my $h,'<:raw',$f) or next; my $t = do { local $/; <$h> };
  my ($b) = $t =~ /(^\[node name="[^"]+" type="[^"]+"[^\]]*\]\n(?:(?!\[)[^\n]*\n)*)/m or next;
  next if $b =~ /parent=/;
  my %a = map { $_ => ($b =~ /^$_ = ([-\d.]+)/m ? $1 : 0) } qw(anchor_left anchor_top anchor_right anchor_bottom);
  (my $res = $f) =~ s{^\./}{res://};
  $root{$res} = \%a;
}
for my $f (@files) {
  open(my $h,'<:raw',$f) or next; my $t = do { local $/; <$h> };
  my %ext = $t =~ /^\[ext_resource type="PackedScene"[^\]]*path="([^"]+)" id="([^"]+)"\]/mg; %ext = reverse %ext;
  for my $b (split /(?=^\[)/m, $t) {
    next unless $b =~ /^\[node name="([^"]+)"[^\]]*instance=ExtResource\("([^"]+)"\)/;
    my ($name, $id) = ($1, $2);
    next unless $b =~ /^layout_mode = 0/m;
    my $r = $root{$ext{$id} // ''} or next;
    my @lost = grep { $r->{$_} != 0 && $b !~ /^$_ = /m } qw(anchor_left anchor_top anchor_right anchor_bottom);
    print "$f  $name -> $ext{$id}  теряет: @lost\n" if @lost;
  }
}
