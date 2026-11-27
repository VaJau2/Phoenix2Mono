# Архитектура Phoenix 2 Mono

> Документ описывает проект на **Godot 4.7.2 .NET (C#)** после миграции с Godot 3.6 Mono (ветка `phnx-godot-4-update-2`, план и итоги — [migration/plan.md](migration/plan.md), баги миграции — [migration/bugs.md](migration/bugs.md)).
> Предназначен в первую очередь для ИИ-агентов и разработчиков, которые вносят правки.
> Пути указаны относительно корня репозитория.

## 1. Общие сведения

| Параметр | Значение |
|---|---|
| Движок | Godot 4.7.2 .NET (до миграции — Godot 3.6 Mono) |
| Язык | C# (`Godot.NET.Sdk/4.7.2`, `net8.0`, `LangVersion=latest` — используются primary constructors, collection expressions `[]`) |
| GDScript | Отдельных `.gd` в игре нет (только инструменты в `docs/migration/tools/`); ~35 встроенных (`sub_resource type="GDScript"`) мини-скриптов в сценах |
| Код | 337 `.cs` файлов, ~33 тыс. строк, всё в `assets/scripts/` |
| Сцены | 352 `.tscn`: 12 уровней в `scenes/`, тестовые в `scenes/test/`, префабы в `objects/` |
| Рендер | Forward+ (по умолчанию), окружение по умолчанию `res://default_env.tres`, у уровней — свои `WorldEnvironment` |
| Физика | Jolt Physics (`3d/physics_engine`) — на GodotPhysics рэгдоллы взрываются |
| Автозагрузки (autoload) | **Нет.** Вместо них — синглтон `Global` (обычный C# класс) и статические классы |
| Главная сцена | `scenes/Main.tscn` |
| Языки | ru (fallback), en — собственная система на JSON, не `TranslationServer` |

### Структура папок

```
assets/
  animations/   анимации игрока
  audio/        звуки и музыка (wav, mp3, ogg)
  dialogues/    диалоги в JSON: ru/, en/, configs/ (настройки озвучки)
  lang/         тексты интерфейса и предметов: ru/*.json, en/*.json
  materials/    материалы, шейдеры (.gdshader), окружения, шрифты, физ. материалы
  models/       3D-модели (.glb, .fbx, .obj)
  scripts/      весь C# код (см. раздел 3)
  textures/
objects/        префабы-сцены: персонажи, оружие, интерфейс, пропсы, тайлы GridMap, тестовые объекты
scenes/         уровни (Main.tscn + 12 уровней), levels.json, test/
docs/           документация; docs/migration/ — миграция на Godot 4 и её инструменты (в билд не входит)
```

## 2. Жизненный цикл игры

### Main.tscn

```
Main (Node, скрипт LevelsLoader.cs)
├── Menu (CanvasLayer)
│   ├── MainMenu         (objects/interface/menus/MainMenu.tscn; DEV-меню Testing — выбор уровня и расы)
│   └── SettingsMenu
├── SaveNode (Node, SaveNode.cs, группа "savable") — инвентарь/переменные между уровнями
├── Testing (objects/testing/Testing.tscn) — отладочные команды F1–F3, тестовая камера
└── Scene  ← текущий уровень добавляется сюда динамически
```

- **`LevelsLoader`** (`assets/scripts/system/LevelsLoader.cs`) — загрузчик уровней, висит на корне `Main`.
  - Список уровней — `scenes/levels.json` (номер → имя `.tscn`). Индекс 0 = главное меню.
  - Асинхронная загрузка: `ResourceLoader.LoadThreadedRequest` + `_Process` → `LoadThreadedGetStatus`/`LoadThreadedGet`.
  - После загрузки: инстанс сцены добавляется в `/root/Main`, сигналы `LevelLoaded`, `SaveDataLoaded`.
  - При загрузке сохранения: удаляет объекты из `deletedObjects`, создаёт `Created_*` объекты, затем через кадр вызывает `ISavable.LoadData` у каждого.
  - Переключает меню: главное ↔ меню паузы, показывает экран загрузки и меню смерти.
- Уровень обязан содержать узел `PlayerSpawner` — он инстанцирует `objects/characters/Player/Player_<Race>.tscn` в зависимости от `Global.playerRace`.
- Тестовые сцены (`scenes/test/*`) можно запускать напрямую — `LevelsLoader._Ready` находит `/root/Main/Scene` (`TestSceneLoader.cs`).

### Global (синглтон)

`assets/scripts/system/Global.cs` — `Global.Get()`:
- ссылка на текущего игрока `player`, раса `playerRace`, флаг паузы, `Settings`;
- `SetPause()` — пауза дерева + музыка в группе `unpaused_sound` + режим мыши;
- `LoadJsonFile()` — чтение JSON из `res://` (`FileAccess`, кэш на **один** последний файл);
- `ToTimer()` — awaitable таймер (используется как `await global.ToTimer(1f)`);
- список удалённых объектов уровня `deletedObjects` для сохранений;
- работа с файлами сохранений `user://saves/` (`FileAccess`, `DirAccess`).

Многие классы обращаются к игроку через `Global.Get().player` (часто через `static` свойство-обёртку).

## 3. Подсистемы кода (`assets/scripts/`)

| Папка | Назначение | Ключевые классы |
|---|---|---|
| `system/` | Ядро: синглтон, загрузка уровней, настройки | `Global`, `LevelsLoader`, `Settings` (ConfigFile), `TestSceneLoader` |
| `characters/` | База персонажей | `Character : CharacterBody3D, ISavable` — здоровье, урон, `Velocity`, `impulse`, сигналы `TakenDamage`, `DieEvent`, `ChangeMayMove`, `IsCame`, `DoorTeleporting`; `EnemiesManager`, `Target` |
| `characters/player/` | Игрок | `Player : Character` + компоненты (см. ниже); расы `Player_Earthpony/Pegasus/Unicorn`; магия единорога `Unicorn/` |
| `characters/NPC/` | NPC | `NPC : Character`, компоненты в `Components/` (оружие, лицо, торговля, патруль, укрытия, сохранение, скелет `NpcSkeleton`), тела `Bodies/` (Pony, Dragon, MrHandy, RoboEye) |
| `characters/NPC/NpcState/` | ИИ NPC — стейт-машина | `StateMachine`, `AbstractNpcState : Node`, состояния в `States/`, маппинг `SetStateEnum`→`NpcStateEnum`→тип класса через `Type.GetType`, создание через самописный DI |
| `characters/Moving/` | Перемещение NPC | `BaseMovingController`, `NavigationMovingController` (`NavigationServer3D.MapGetPath`, обход застреваний — B-53) |
| `furniture/` | Интерактивные объекты | `FurnBase : StaticBody3D, IInteractable`, двери, сундуки, терминалы, радио (`Radio/`), комнаты, `PhoenixSystem`, `Mirror` |
| `gun/`, `background/shells/` | Пули, частицы выстрела, гильзы | `Bullet : Area3D`, `GunParticles`, `ShellsManager` |
| `interface/` | UI | меню (`Menus/`), сохранение/загрузка (`Menus/Saving/`), настройки, инвентарь (`inventory/`), эффекты предметов (`inventory/effects/`), диалоги (`Dialogue/`), субтитры, сообщения, HUD |
| `triggers/` | Скриптовые события уровней | `TriggerBase : Node, ISavable, IActivated` и ~60 наследников, кинематики `cinematic/`, музыка `music/`, специфичные для уровней `snow_base/`, `wasteland/`, `zebra_base/`, `training/` |
| `audio/` | Аудио-эффекты экипировки | `AudioEffectsController`, фабрики эффектов шины |
| `background/` | Окружение | `Explosion`, `Smoke`, `RadiationArea`, `GridmapStepSound : GridMap`, `Water` |
| `utils/` | Утилиты | `ConvertUtils` (парсинг Vector3 из строки), `SaveNode`, `SaveActive`, `SavePosition`, `AudioPlayerCommon`, `DI/`, `testing/`; помощники Godot 4: `VariantExtensions`, `BoundCall`, `JoltScaleFix` (см. раздел 8) |

### Игрок

`objects/characters/Player/Player_<Race>.tscn` → скрипт `Player_<Race> : Player : Character : CharacterBody3D`.

Компоненты (поля `Player`, ищутся через `GetNode` в `_Ready`):
`PlayerCamera` (Camera3D, взаимодействие), `PlayerBody` (Node3D, анимации через `AnimationTree`), `PlayerHead`, `PlayerLegs`, `PlayerThirdPerson`, `PlayerWeapons` (CollisionShape3D), `PlayerInventory` (обычный класс), `PlayerStealth`, `PlayerRadiation`, `PlayerDeathManager`, `PlayerDialogueCheck`, `SoundSteps` (RayCast3D), `LightsCheck`, `AudioEffectsController`, `StealthBoyEffect`, `PlayerRagdollPusher`.

Движение: у `Character` своё поле `Velocity` (скрывает свойство `CharacterBody3D.Velocity`, его меняют по компонентам); в движок скорость передаёт `Character.MoveAndSlideVelocity(stopOnSlope, maxSlides)` — аналог `MoveAndSlide` из Godot 3.

Сигналы игрока: `ChangeView`, `FireWithWeapon`, `TakeItem`, `UseItem`, `WearItem`, `UnwearItem`.

Модель тела: `Body` — вид от 1-го лица (слой рендера 2, тень не отбрасывает), `Body_third` — от 3-го лица (слой 3) и тень от 1-го лица. Много жёстких путей в модель: `GetNode<Skeleton3D>("player_body/Armature/Skeleton")`, `.../BoneAttachment 2/shotgunBag` и т. п.

### NPC

- `NPC : Character, IInteractable, IChest` + дочерние компоненты (`weapons`, `seekArea`, `covers`, `patroling`, `movingController`, `stateMachine` и др.).
- **Стейт-машина** (`StateMachine.cs`): в инспекторе задаются два параллельных массива `availableSetStates` / `availableNpcStates` (`[Export] Array<enum>`); состояние создаётся через `DependencyInjection.CreateClass` — рефлексия по конструктору, зависимости берутся из `Godot.Collections.Dictionary` по имени типа (`NpcStateDependencies`).
- **Сохранение NPC** сохраняет даже динамически подключённые сигналы (`GetSignalConnectionList` → `Connect` при загрузке) и позиции `PhysicalBone3D` рэгдолла после смерти.
- Тела: `PonyBody`, `DragonBody`, `MrHandyBody`, `RoboEyeBody` — анимации и визуал.

### Триггеры

- Базовый класс `TriggerBase`: `IsActive`, `DeleteAfterTrigger`, метод `_on_activate_trigger()`; после срабатывания удаляется и записывается в `Global.deletedObjects` (чтобы не появиться после загрузки).
- `ActivateOtherTrigger` включает/выключает другие триггеры по `Array<NodePath>`; `_on_body_entered(Node body)` — срабатывание при входе игрока.
- **Связь со сценами — через сигналы, подключённые в редакторе** (`[connection ...]` в `.tscn`): 431 подключение, из них 116 с `binds`. Самые частые: `body_entered` (106), `mouse_entered/exited` (78/78). Методы подключаются по имени `_on_<node>_<signal>` — переименование ломает сцену без ошибки компиляции.
- Кинематики (`triggers/cinematic/`): движение камеры/персонажей по `Path3D`/`PathFollow3D`, `Cutscene`.

### Интерфейс

- `MenuManager` (static) — стек «одно открытое меню + фоновое»; меню реализуют `IMenu`.
- Меню: `MainMenu`, `PauseMenu`, `DeathMenu`, `SettingsMenu` (подменю `ControlsSubmenu`, `DifficultySubmenu`, `SettingsSubmenu`), `LoadMenu`, `SaveMenu`, `TestingLevelsMenu`.
- **Инвентарь**: `InventoryMenu` + режимы `InventoryMode` (`UsualMode`, `ChestMode`, `TradeMode`), обработчики `BindsHandler`, `UseHandler`; данные предметов — `ItemJSON` из `assets/lang/<lang>/items.json`.
- **Эффекты предметов** (`inventory/effects/`): `EffectHandler` + классы `Effect` (обычные C# классы, не узлы), пары «эффект/пост-эффект».
- **Диалоги** (`interface/Dialogue/`): `DialogueMenu` читает `assets/dialogues/<lang>/<npc>/<code>.json` (граф узлов `nodes` с `options`/`next`), запускает скрипты по имени через `Type.GetType("DialogueScripts." + name)` + `Activator.CreateInstance` (`DialogueScripts/*.cs`, интерфейс `IDialogueScript`). `Subtitles` работает аналогично для фоновых реплик. Озвучка — `DialogueAudio` + `assets/dialogues/configs`.
- **Локализация**: `InterfaceLang` (static) — `GetPhrase(file, section, phrase)` из `assets/lang/<lang>/<file>.json`; `LabelLang` для статических лейблов. Плейсхолдеры `#...#` заменяются на названия клавиш.
- После пересохранения сцен интерфейса редактором — `perl docs/migration/tools/check_layout.pl` (редактор дописывает экземплярам `anchors_preset = 0`/`layout_mode = 0`, меню уезжают за экран, B-7/B-26).

## 4. Сохранения

- Файл: `user://saves/<name>.sav`, `FileAccess.OpenCompressed` (сжатый формат Godot, совместим с `File.OpenCompressed` из Godot 3).
- Формат — построчный:
  1. имя сохранения
  2. дата
  3. номер уровня (`LevelsLoader.tempLevelNum`)
  4. `autosave_<slot>`
  5. раса
  6. `Json.Stringify(Global.deletedObjects)` — пути удалённых узлов
  7. `Json.Stringify(objectsData)` — словарь «путь узла → данные `GetSaveData()`»
- В сохранение попадают **все узлы группы `savable`**, реализующие `ISavable` (`GetSaveData()` / `LoadData(Dictionary)`) — ~60 классов.
- Объекты, созданные во время игры, называются `Created_*`; для них сохраняются `fileName` (путь к `.tscn`) и `parentPath`.
- Векторы сохраняются строкой (`Vector3.ToString()`) и парсятся `ConvertUtils.ParseToVector3`.
- Enum'ы в `Godot.Collections.Dictionary` кладутся числами (`(int)`), читаются через `.AsInt32()`.
- **Сохранения Godot 3 загружаются** — не переименовывать узлы уровней и не менять формат (пути узлов — ключи в сохранении). Образцы старых сохранений — `docs/migration/saves`, проверка — смоук-тест с `--saves`.
- `SaveNode` в `Main` переносит инвентарь и «сохраняемые переменные» между уровнями.
- Настройки — отдельно, `ConfigFile` в `Settings.cs`.

## 5. Ассеты и сцены

- Типы узлов в сценах (топ): `CollisionShape3D` 1156, `MeshInstance3D` 1147, `Node3D` 919, `StaticBody3D` 392, `Label` 351, `Sprite3D` 168, `AudioStreamPlayer3D` 130, `Area3D` 128, `PhysicalBone3D` 97.
- Уровни очень большие (несколько МБ текста) — меши часто встроены прямо в сцену (`ArrayMesh` sub_resource, ~830).
- Уровни собираются из **GridMap**-тайлов (`objects/props/tiles/`).
- Модели: 226 `.glb`, 51 `.fbx`, 7 `.obj`. Бинарные ресурсы Godot 3 (`.material`, `.res`, `.mesh`, `.anim`, `.phymat`, `.meshlib`) при миграции переведены в текстовые `.tres` (`docs/migration/tools/convert_binary_resources.*`).
- Шейдеры: 3 `.gdshader` (`contrast`, `fisheye`, `godrays`) + встроенный в `assets/dialogues/blur.tres`.
- Шрифты: `FontFile` + `FontVariation`.
- Текстуры в 3D — `texture_filter = Nearest` в материалах (пиксельный стиль), импорт без VRAM-сжатия.
- Свет Omni/Spot пересчитан под формулу затухания Godot 4 (`fix_lights.pl`, исходные значения — `metadata/g3_*`); новые источники настраивать на глаз.

## 6. Слои и ввод

Физические слои: 1 Default, 2 Ragdoll & Floor, 3 Interactive, 4 Exit, 5 Door, 6 Camera, 7 Corpse (кости трупов, `NpcSkeleton.CORPSE_LAYER`).
Слои рендера: 2 First Person, 3 Third Person.

Столкновения в Godot 4 односторонние: тело/зона замечает объект, только если его слой есть в её собственной маске (B-49/B-50). Кости живых игрока и NPC — слой 0, предметы — слой 1, трупы с предметами не сталкиваются; трупы и физические предметы игрок толкает кодом (`PlayerRagdollPusher`, `Player.PushRigidBodies`).

Действия ввода (`project.godot`): `ui_*`, `jump`, `dash`, `crouch`, `changeView`, `legsHit`, `task`, `use`, `getGun`, `inventory`, `autoheal`, `ui_quicksave`, `ui_quickload`, `test_camera_*`. Переназначение клавиш — `ControlsSubmenu` (через `InputMap` и `Keycode`). В `_Input` проверять само событие (`@event.IsActionPressed`), а не `Input.IsActionJustPressed` (B-39).

## 7. Соглашения кода

- Комментарии на русском и короткие (одна-две строки; подробности — в документации в `docs/`), имена на английском, смешанный стиль: публичные поля часто `camelCase`, методы — `PascalCase`, обработчики сигналов из редактора — `_on_<node>_<signal>`.
- Все классы-узлы — `partial`; сигналы — `[Signal] delegate void <Name>EventHandler(...)`, отправка — `EmitSignal(SignalName.<Name>, ...)`.
- Узлы ищутся строковыми путями (`GetNode<T>("...")` — ~690 вызовов), почти нет `[Export] NodePath`.
- Асинхронность через `async void` + `await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame)` и `await global.ToTimer(...)`.
- Подключение сигналов в коде: `obj.Connect(Class.SignalName.X, new Callable(this, nameof(Method)))`; с аргументами — `utils/BoundCall.cs` (в C# нет `Callable.Bind`).
- Интерфейсы поведения: `IInteractable`, `ISavable`, `IChest`, `IMenu`, `IActivated`, `IDoorTeleport`, `ITrader`.
- Отладка: `objects/testing/Testing.tscn` (F1 — лечение, F2 — убить врагов, F3 — убить игрока, тестовая камера), `TestingLevelsMenu` — быстрый выбор уровня.

## 8. Особенности Godot 4, важные при правках

Краткий список; подробности — в разделе «Важное» [CLAUDE.md](../CLAUDE.md) и в записях [migration/bugs.md](migration/bugs.md).

- **`Variant` не `IConvertible`**: только `.AsInt32()`/`.AsBool()` и т. п.; проверки `is/as` — `utils/VariantExtensions.cs`.
- Godot 4 **молча игнорирует неизвестные свойства в сценах**, а у части enum'ов сдвинулись номера — при странном поведении узла проверить `docs/migration/tools/check_props.gd` и значения enum'а.
- У примитивов (`SphereMesh`, `BoxShape3D` и т. п.) вдвое меньше умолчания — новым задавать размер явно.
- Пустая строка в экспортируемом `string` редактор выбрасывает из сцены — если `null` и `""` значат разное, задавать непустое значение по умолчанию (B-38).
- `AnimationTree` — `deterministic = false` (как смешивание в Godot 3).
- Jolt: неравномерный масштаб коллизий исправляет на лету `utils/JoltScaleFix.cs` (B-14); сетки коллизии — `backface_collision = true` (B-29); суставам рэгдоллов softness/relaxation не задавать (B-56). В C# для узлов со скриптом класс проверять через `IsClass("...")`, а не `is`.
- Прозрачные материалы в Godot 4 отбрасывают тень — для «невидимости» выключать `CastShadow` (B-55).

## 9. Сборка, проверка, экспорт

- Сборка C#: `dotnet build "Phoenix 2 Mono.csproj"`.
- Смоук-тест всех уровней (обязателен после правок кода) и прочие проверки — см. раздел «Сборка и запуск» в [CLAUDE.md](../CLAUDE.md) и шапки файлов в `docs/migration/tools/`.
- Экспорт: пресет «Windows Desktop» (`export_presets.cfg`), шаблоны 4.7.2 .NET. В пак входят ресурсы и `*.json` (`include_filter`); исключены `docs/` и четыре давно сломанных неиспользуемых префаба (`PropDamage`, `CP`, `roof`, `wall-gun`). Из командной строки: `... --headless --path . --export-release "Windows Desktop" "<путь>/Phoenix 2 Mono.exe"`. Рядом с exe создаются `.pck` и папка `data_Phoenix 2 Mono_windows_x86_64` (сборки .NET) — распространять вместе.
