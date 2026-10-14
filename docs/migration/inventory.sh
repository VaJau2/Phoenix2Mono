#!/usr/bin/env bash
# Считает в C# коде места, использующие API Godot 3, которые меняются/удалены в Godot 4.
# Запуск из корня репозитория (Git Bash): bash docs/migration/inventory.sh
# По мере миграции счётчики должны стремиться к нулю.

cd "$(dirname "$0")/../.." || exit 1
SRC=assets/scripts

p() {
    local files hits
    files=$(grep -rlE "$2" --include=*.cs "$SRC" | wc -l)
    hits=$(grep -rhoE "$2" --include=*.cs "$SRC" | wc -l)
    printf "| %-44s | %5s | %5s |\n" "$1" "$files" "$hits"
}

echo "| Паттерн | Файлов | Вхождений |"
echo "|---|---:|---:|"
p "class с базой, без partial (кандидаты)"  '^\s*public (abstract )?class \w+\s*:\s*[A-Z]'
printf "| %-44s | %5s | %5s |\n" "[Signal] delegate без суффикса EventHandler" \
    "$(grep -rlE 'delegate void \w+\(' --include=*.cs "$SRC" | xargs -r grep -LE 'delegate void \w+EventHandler\(' | wc -l)" \
    "$(grep -rhE 'delegate void \w+\(' --include=*.cs "$SRC" | grep -vc 'EventHandler(')"
p "Connect/Disconnect/IsConnected (3.x)"  '\.(Connect|Disconnect|IsConnected)\([^)]*,\s*this\s*,'
p "EmitSignal(nameof(...))"               'EmitSignal\(nameof'
p "ToSignal(..., \"idle_frame\")"         '"idle_frame"'
p ".Instance() у PackedScene"             '\.Instance(<[^>]*>)?\(\)'
p "Spatial"                               '\bSpatial\b'
p "KinematicBody / MoveAndSlide(3 арг.)"  'KinematicBody|MoveAndSlide\('
p "3D-узлы без суффикса 3D"               '\b(StaticBody|RigidBody|Area|CollisionShape|RayCast|Camera|MeshInstance|Skeleton|BoneAttachment|PhysicalBone|Position3D|Particles|OmniLight|SpotLight|DirectionalLight|Path|PathFollow|VisibilityNotifier|Listener)\b'
p "Transform (→ Transform3D)"            '\bTransform\b'
p "GlobalTranslation/Translation"         '\b(Global)?Translation\b'
p "x/y/z/origin/basis в нижнем регистре"  '\.(x|y|z|origin|basis)\b'
p "_Process/_PhysicsProcess(float)"       'override void _(Physics)?Process\(float'
p "File / Directory"                      '\b(new File|new Directory|File\.ModeFlags)'
p "JSON.Parse / JSON.Print"               'JSON\.(Parse|Print)'
p "ResourceInteractiveLoader"             'ResourceInteractiveLoader|LoadInteractive'
p "PauseMode"                             'PauseMode'
p "Scancode / ButtonList / KeyList"       '[Ss]cancode|ButtonList|KeyList'
p "Rect* свойства Control"                '\bRect(MinSize|Size|Position|GlobalPosition|Scale|Rotation|PivotOffset)\b'
p "Godot.Object"                          'Godot\.Object|\bObject\.IsInstanceValid'
p "SpatialMaterial / SetShaderParam"      'SpatialMaterial|SetShaderParam|GetShaderParam'
p "Dictionary.Contains(\"...\")"          '\.Contains\("'
p "String-ext: BeginsWith/Empty()"        '\.(BeginsWith|Empty\(\))'
