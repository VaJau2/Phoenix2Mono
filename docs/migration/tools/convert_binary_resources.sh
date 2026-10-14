#!/usr/bin/env bash
# Этап 0.4 миграции: перевод бинарных ресурсов Godot 3 в текстовый .tres.
#   1. convert_binary_resources.gd пересохраняет файлы (и выносит встроенные текстуры в .png);
#   2. ссылки "res://старый" -> "res://новый" заменяются во всех .tscn/.tres;
#   3. исходные бинарные файлы удаляются из git.
# Пути, которые собираются в C# коде динамически, правятся вручную (см. docs/migration/inventory.md §6).
#
# Запуск из корня репозитория (Git Bash), редактор Godot 3 должен быть закрыт:
#   bash docs/migration/tools/convert_binary_resources.sh [путь к Godot 3 exe]

set -euo pipefail
cd "$(dirname "$0")/../../.."

GODOT=${1:-/d/Godot/Godot_v3.6.3-stable_mono_win64/Godot_v3.6.3-stable_mono_win64.exe}
WORK=$(mktemp -d)
LIST=$WORK/list.txt
MAP=$WORK/map.txt
LOG=$WORK/convert.log

git ls-files '*.material' '*.res' '*.mesh' '*.meshlib' '*.anim' '*.phymat' | sed 's|^|res://|' > "$LIST"
echo "Файлов к конвертации: $(wc -l < "$LIST")"

"$GODOT" --no-window --path . -s docs/migration/tools/convert_binary_resources.gd \
    "--list=$(cygpath -m "$LIST")" "--map=$(cygpath -m "$MAP")" > "$LOG" 2>&1 || true

grep -E '^(DONE|FAIL|WARN)' "$LOG" || true
if ! grep -q '^DONE .* failed=0 ' "$LOG"; then
    echo "Конвертация завершилась с ошибками, лог: $LOG" >&2
    exit 1
fi

# sed-скрипт замены путей: экранируем спецсимволы, разделитель — |
SED=$WORK/replace.sed
while IFS=$'\t' read -r src dst; do
    esc_src=$(printf '%s' "$src" | sed 's/[.[\*^$|]/\\&/g')
    esc_dst=$(printf '%s' "$dst" | sed 's/[&|\\]/\\&/g')
    printf 's|"%s"|"%s"|g\n' "$esc_src" "$esc_dst" >> "$SED"
done < "$MAP"

# Где искать ссылки: все отслеживаемые .tscn/.tres + только что созданные .tres
{
    git ls-files -z '*.tscn' '*.tres'
    cut -f2 "$MAP" | sed 's|^res://||' | tr '\n' '\0'
} | sort -zu > "$WORK/files.txt"
# grep возвращает 1 для файлов без совпадений — это не ошибка
xargs -0 grep -lZF -e '.material"' -e '.res"' -e '.mesh"' -e '.meshlib"' -e '.anim"' -e '.phymat"' < "$WORK/files.txt" > "$WORK/matched.txt" || true
xargs -0 -r sed -i -f "$SED" < "$WORK/matched.txt"

# Удаляем исходные бинарные файлы
cut -f1 "$MAP" | sed 's|^res://||' | tr '\n' '\0' | xargs -0 git rm -q --

echo "Готово. Соответствия путей: $MAP, лог: $LOG"
echo "Оставшиеся ссылки на бинарные ресурсы в сценах:"
git ls-files -z '*.tscn' '*.tres' | xargs -0 grep -nE 'path="res://[^"]+\.(material|res|mesh|meshlib|anim|phymat)"' || echo "  нет"
