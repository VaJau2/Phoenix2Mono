# Phoenix 2 Mono

3D-игра (пони, постапокалипсис) на **Godot 3.6 Mono, C#**. Идёт подготовка к миграции на Godot 4.x.

## Документация

- [docs/architecture.md](docs/architecture.md) — архитектура: жизненный цикл, подсистемы, сохранения, ассеты, соглашения.
- [docs/migration/plan.md](docs/migration/plan.md) — план миграции на Godot 4.7.2 по этапам, принятые решения, чек-лист тестирования.
- [docs/migration/inventory.md](docs/migration/inventory.md) — что затронет переход на Godot 4, с оценкой риска.
- `bash docs/migration/inventory.sh` — пересчитать места в C# коде, использующие API Godot 3.

Читай `architecture.md` перед изменениями в незнакомой подсистеме.

## Сборка и запуск

- Сборка C# без редактора: `dotnet build "Phoenix 2 Mono.csproj"` (сейчас: 0 ошибок, ~200 предупреждений — это норма).
- Редактор Godot 3: `D:\Godot\Godot_v3.6.3-stable_mono_win64\Godot_v3.6.3-stable_mono_win64.exe` (вывод в консоль не идёт — перенаправлять в файл).
- Godot 3 без окна для скриптов: `... .exe --no-window --path . -s <script.gd>` (скрипт `extends SceneTree`).
- Целевой Godot 4: `D:\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe` (`--headless`).
- Главная сцена `scenes/Main.tscn`; уровни — `scenes/levels.json`; тестовые сцены `scenes/test/` можно запускать напрямую.
- Отладка в игре: F1 — лечение, F2 — убить врагов, F3 — убить игрока (`objects/testing/Testing.tscn`).

## Важное

- Окончания строк — LF (`.gitattributes`), Godot сам пишет LF.
- Комментарии в коде — на русском, как в остальном проекте.
- Сцены подключают сигналы к методам `_on_<node>_<signal>` по имени — переименование таких методов ломает сцены без ошибки компиляции.
- Всё, что должно сохраняться, реализует `ISavable` и состоит в группе `savable`; объекты, созданные в рантайме, называются `Created_*`.
- Ветка `origin/PHNX-154-godot-4-update` — старая попытка миграции на Godot 4.2. Не мержить, использовать как справочник (`git show origin/PHNX-154-godot-4-update:<путь>`).
