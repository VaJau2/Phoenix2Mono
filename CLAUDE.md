# Phoenix 2 Mono

3D-игра (пони, постапокалипсис) на C#. Основная ветка — **Godot 3.6 Mono**; в ветке `phnx-godot-4-update-2` идёт миграция на **Godot 4.7.2 .NET** (этапы 0–3 выполнены, дальше — тестирование и баги, этап 4).

## Документация

- [docs/migration/plan.md](docs/migration/plan.md) — план миграции по этапам, **итоги каждого этапа**, что проверить в игре, чек-лист тестирования. Читать первым при работе над миграцией.
- [docs/migration/bugs.md](docs/migration/bugs.md) — баги, найденные при тестировании на Godot 4 (если файл есть).
- [docs/architecture.md](docs/architecture.md) — архитектура: жизненный цикл, подсистемы, сохранения, ассеты, соглашения (описана версия на Godot 3, структура та же).
- [docs/migration/inventory.md](docs/migration/inventory.md) — исходная инвентаризация затронутого API.
- `docs/migration/tools/` — инструменты миграции (описание в шапке каждого файла).

Читай `architecture.md` перед изменениями в незнакомой подсистеме.

## Сборка и запуск (Godot 4)

- Сборка C#: `dotnet build "Phoenix 2 Mono.csproj"` (0 ошибок).
- Godot 4: `D:\Godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . ...`
- Скрипт без редактора: `... --headless --path . -s <абсолютный путь к .gd> -- <аргументы>` (скрипт `extends SceneTree`, аргументы — `OS.get_cmdline_user_args()`).
- Проверка загрузки всех сцен: `docs/migration/tools/load_all_4.gd`; поиск молча потерянных свойств: `docs/migration/tools/check_props.gd`.
- **Смоук-тест уровней** (после правок кода — обязательно): `... --headless --fixed-fps 60 --path . -s D:/Godot/projects/Phoenix2Mono/docs/migration/tools/smoke_test.gd -- [--levels=1,7] [--frames=900] [--saves] > smoke.log 2>&1` (`--saves` — F5/F9 на каждом уровне и загрузка старых сохранений из `docs/migration/saves`; папка `user://saves` восстанавливается после теста, при аварийном завершении копия остаётся в `user://saves_smoke_backup`), затем `perl docs/migration/tools/smoke_report.pl smoke.log` — ошибки по уровням и месту в коде. Все 12 уровней — ~4 минуты. Предупреждения Jolt о `softness/relaxation` и OGG о комментариях — известный шум.
- Godot 3 (для сравнения): `D:\Godot\Godot_v3.6.3-stable_mono_win64\Godot_v3.6.3-stable_mono_win64.exe --no-window --path . -s <script.gd>`; вывод в консоль не идёт — перенаправлять в файл.
- Главная сцена `scenes/Main.tscn`; уровни — `scenes/levels.json`; тестовые сцены `scenes/test/`.
- Отладка в игре: F1 — лечение, F2 — убить врагов, F3 — убить игрока (`objects/testing/Testing.tscn`).
- Перед запуском инструментов, которые пишут файлы, Godot и Rider должны быть закрыты (Rider блокирует файлы при индексации).

## Важное

- Окончания строк — LF (`.gitattributes`), Godot сам пишет LF.
- Комментарии в коде — на русском, как в остальном проекте.
- Сцены подключают сигналы к методам `_on_<node>_<signal>` по имени — переименование таких методов ломает сцены без ошибки компиляции.
- Всё, что должно сохраняться, реализует `ISavable` и состоит в группе `savable`; объекты, созданные в рантайме, называются `Created_*`. Старые сохранения (Godot 3) должны загружаться — не переименовывать узлы уровней и не менять формат сохранения.
- **Godot 4 молча игнорирует неизвестные свойства в сценах** — при странном поведении узла сначала проверить `check_props.gd`.
- **У части enum'ов в Godot 4 сдвинулись номера** (имя свойства то же, число означает другое): `emission_shape` частиц, `specular_mode`/`diffuse_mode` материалов, `stretch_mode` у TextureRect — исправлено `fix_enums.pl`/`fix_props.pl`. При странном поведении — сверить значения enum'а в Godot 3 и 4. Пересохранение редактором закрепляет неверные значения и выбрасывает «лишние» свойства (так терялись `emission_box_extents`).
- **`Variant` не `IConvertible`**: не использовать `Convert.ToInt32(dict["x"])`, только `.AsInt32()`/`.AsBool()` и т. п. Для проверок `is/as` — `utils/VariantExtensions.cs`. Подключения сигналов с аргументами — `utils/BoundCall.cs` (в C# нет `Callable.Bind`).
- `AnimationTree` — `deterministic = false` (как смешивание в Godot 3: дорожки, которых нет в анимации, не тянутся к позе покоя). Новым `AnimationTree` ставить так же.
- Физика — Jolt (на GodotPhysics рэгдоллы взрываются). Текстуры в 3D — `texture_filter = Nearest` в материалах (пиксельный стиль), импорт без VRAM-сжатия (`detect_3d/compress_to=0`).
- Ветка `origin/PHNX-154-godot-4-update` — старая попытка миграции на Godot 4.2. Не мержить, использовать как справочник (`git show origin/PHNX-154-godot-4-update:<путь>`).
