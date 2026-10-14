# Архитектура Phoenix 2 Mono

> Документ описывает состояние проекта на **Godot 3.6 Mono (C#)** (локально установлен 3.6.3) перед миграцией на Godot 4.
> Предназначен в первую очередь для ИИ-агентов и разработчиков, которые будут вносить правки при обновлении.
> Пути указаны относительно корня репозитория.

## 1. Общие сведения

| Параметр | Значение |
|---|---|
| Движок | Godot 3.6 Mono (в истории — обновление до 3.6.1, установлен 3.6.3) |
| Язык | C# (`Godot.NET.Sdk/3.3.0`, `net472`, `LangVersion=latest` — используются C# 12 фичи: primary constructors, collection expressions `[]`) |
| GDScript | Отдельных `.gd` файлов нет; есть ~35 встроенных (`sub_resource type="GDScript"`) мини-скриптов в сценах и 3 битые ссылки на несуществующие `.gd` (см. [migration/inventory.md](migration/inventory.md)) |
| Код | 331 `.cs` файл, ~31 тыс. строк, всё в `assets/scripts/` |
| Сцены | 352 `.tscn`: 12 уровней в `scenes/`, 8 тестовых в `scenes/test/`, префабы в `objects/` |
| Рендер | GLES3 (по умолчанию), окружение `res://default_env.tres` |
| Автозагрузки (autoload) | **Нет.** Вместо них — синглтон `Global` (обычный C# класс) и статические классы |
| Главная сцена | `scenes/Main.tscn` |
| Языки | ru (fallback), en — собственная система на JSON, не `TranslationServer` |

### Структура папок

```
assets/
  animations/   .anim (бинарные) — анимации игрока
  audio/        звуки и музыка (wav, mp3, ogg)
  dialogues/    диалоги в JSON: ru/, en/, configs/ (настройки озвучки)
  lang/         тексты интерфейса и предметов: ru/*.json, en/*.json
  materials/    материалы (.material бинарные, .tres), шейдеры (.shader), окружения, шрифты, физ. материалы
  models/       3D-модели (.glb, .fbx, .obj) + сгенерированные импортом .material/.mesh/.res
  scripts/      весь C# код (см. раздел 3)
  textures/
objects/        префабы-сцены: персонажи, оружие, интерфейс, пропсы, тайлы GridMap, тестовые объекты
scenes/         уровни (Main.tscn + 12 уровней), levels.json, test/
```

## 2. Жизненный цикл игры

### Main.tscn

```
Main (Node, скрипт LevelsLoader.cs)
├── Menu (CanvasLayer)
│   ├── MainMenu         (objects/interface/menus/MainMenu.tscn)
│   └── SettingsMenu
├── SaveNode (Node, SaveNode.cs, группа "savable") — инвентарь/переменные между уровнями
├── Testing (objects/testing/Testing.tscn) — отладочные команды F1–F3, тестовая камера
└── Scene  ← текущий уровень добавляется сюда динамически
```

- **`LevelsLoader`** (`assets/scripts/system/LevelsLoader.cs`) — загрузчик уровней, висит на корне `Main`.
  - Список уровней — `scenes/levels.json` (номер → имя `.tscn`). Индекс 0 = главное меню.
  - Асинхронная загрузка через `ResourceLoader.LoadInteractive` + `_Process` → `Poll()` (**в Godot 4 этого API нет**, заменяется на `ResourceLoader.LoadThreadedRequest/LoadThreadedGetStatus`).
  - После загрузки: инстанс сцены добавляется в `/root/Main`, сигналы `LevelLoaded`, `SaveDataLoaded`.
  - При загрузке сохранения: удаляет объекты из `deletedObjects`, создаёт `Created_*` объекты, затем через кадр вызывает `ISavable.LoadData` у каждого.
  - Переключает меню: главное ↔ меню паузы, показывает экран загрузки и меню смерти.
- Уровень обязан содержать узел `PlayerSpawner` — он инстанцирует `objects/characters/Player/Player_<Race>.tscn` в зависимости от `Global.playerRace`.
- Тестовые сцены (`scenes/test/*`) можно запускать напрямую — `LevelsLoader._Ready` находит `/root/Main/Scene` (`TestSceneLoader.cs`).

### Global (синглтон)

`assets/scripts/system/Global.cs` — `Global.Get()`:
- ссылка на текущего игрока `player`, раса `playerRace`, флаг паузы, `Settings`;
- `SetPause()` — пауза дерева + музыка в группе `unpaused_sound` + режим мыши;
- `LoadJsonFile()` — чтение JSON из `res://` (кэш на **один** последний файл);
- `ToTimer()` — awaitable таймер (используется как `await global.ToTimer(1f)`);
- список удалённых объектов уровня `deletedObjects` для сохранений;
- работа с файлами сохранений `user://saves/` (`File`, `Directory` — **API удалены в Godot 4**).

Многие классы обращаются к игроку через `Global.Get().player` (часто через `static` свойство-обёртку).

## 3. Подсистемы кода (`assets/scripts/`)

| Папка | Строк | Назначение | Ключевые классы |
|---|---|---|---|
| `system/` | 885 | Ядро: синглтон, загрузка уровней, настройки | `Global`, `LevelsLoader`, `Settings` (ConfigFile), `TestSceneLoader` |
| `characters/` | ~330 | База персонажей | `Character : KinematicBody, ISavable` — здоровье, урон, `Velocity`, `impulse`, сигналы `TakenDamage`, `DieEvent`, `ChangeMayMove`, `IsCame`, `DoorTeleporting`; `EnemiesManager`, `Target` |
| `characters/player/` | ~5300 | Игрок | `Player : Character` + компоненты (см. ниже); расы `Player_Earthpony/Pegasus/Unicorn`; магия единорога `Unicorn/` |
| `characters/NPC/` | ~3400 | NPC | `NPC : Character`, компоненты в `Components/` (оружие, лицо, торговля, патруль, укрытия, сохранение, скелет), тела `Bodies/` (Pony, Dragon, MrHandy, RoboEye) |
| `characters/NPC/NpcState/` | ~960 | ИИ NPC — стейт-машина | `StateMachine`, `AbstractNpcState : Node`, состояния в `States/`, маппинг `SetStateEnum`→`NpcStateEnum`→тип класса через `Type.GetType`, создание через самописный DI |
| `characters/Moving/` | 336 | Перемещение NPC | `BaseMovingController`, `NavigationMovingController` (`NavigationServer.MapGetPath`) |
| `furniture/` | ~3400 | Интерактивные объекты | `FurnBase : StaticBody, IInteractable`, двери, сундуки, терминалы, радио (`Radio/`), комнаты, `PhoenixSystem`, `Mirror` |
| `gun/`, `background/shells/` | ~380 | Пули, частицы выстрела, гильзы | `Bullet : Area`, `GunParticles`, `ShellsManager` |
| `interface/` | ~8000 | UI | меню (`Menus/`), сохранение/загрузка (`Menus/Saving/`), настройки, инвентарь (`inventory/`), эффекты предметов (`inventory/effects/`), диалоги (`Dialogue/`), субтитры, сообщения, HUD |
| `triggers/` | ~5800 | Скриптовые события уровней | `TriggerBase : Node, ISavable, IActivated` и ~60 наследников, кинематики `cinematic/`, музыка `music/`, специфичные для уровней `snow_base/`, `wasteland/`, `zebra_base/`, `training/` |
| `audio/` | ~315 | Аудио-эффекты экипировки | `AudioEffectsController`, фабрики эффектов шины |
| `background/` | ~500 | Окружение | `Explosion`, `Smoke`, `RadiationArea`, `GridmapStepSound : GridMap`, `Water` |
| `utils/` | ~720 | Утилиты | `ConvertUtils` (парсинг Vector3 из строки), `SaveNode`, `SaveActive`, `SavePosition`, `AudioPlayerCommon`, `DI/`, `testing/` |

### Игрок

`objects/characters/Player/Player_<Race>.tscn` → скрипт `Player_<Race> : Player : Character : KinematicBody`.

Компоненты (поля `Player`, ищутся через `GetNode` в `_Ready`):
`PlayerCamera` (Camera, взаимодействие), `PlayerBody` (Spatial, анимации через `AnimationTree`), `PlayerHead`, `PlayerLegs`, `PlayerThirdPerson`, `PlayerWeapons` (CollisionShape), `PlayerInventory` (обычный класс), `PlayerStealth`, `PlayerRadiation`, `PlayerDeathManager`, `PlayerDialogueCheck`, `SoundSteps` (RayCast), `LightsCheck`, `AudioEffectsController`, `StealthBoyEffect`.

Движение: `MoveAndSlide` в `Player._PhysicsProcess` (**сигнатура меняется в Godot 4** — скорость становится свойством `Velocity` у `CharacterBody3D`, а в `Character` уже есть своё поле `Velocity` → конфликт имён).

Сигналы игрока: `ChangeView`, `FireWithWeapon`, `TakeItem`, `UseItem`, `WearItem`, `UnwearItem`.

Много жёстких путей в модель: `GetNode<Skeleton>("player_body/Armature/Skeleton")`, `.../BoneAttachment 2/shotgunBag` и т. п. — при реимпорте моделей в Godot 4 имена узлов могут измениться.

### NPC

- `NPC : Character, IInteractable, IChest` + дочерние компоненты (`weapons`, `seekArea`, `covers`, `patroling`, `movingController`, `stateMachine` и др.).
- **Стейт-машина** (`StateMachine.cs`): в инспекторе задаются два параллельных массива `availableSetStates` / `availableNpcStates` (`[Export] Array<enum>`); состояние создаётся через `DependencyInjection.CreateClass` — рефлексия по конструктору, зависимости берутся из `Godot.Collections.Dictionary` по имени типа (`NpcStateDependencies`).
- **Сохранение NPC** сохраняет даже динамически подключённые сигналы (`GetSignalConnectionList` → `Connect` при загрузке) и позиции `PhysicalBone` рэгдолла после смерти.
- Тела: `PonyBody`, `DragonBody`, `MrHandyBody`, `RoboEyeBody` — анимации и визуал.

### Триггеры

- Базовый класс `TriggerBase`: `IsActive`, `DeleteAfterTrigger`, метод `_on_activate_trigger()`; после срабатывания удаляется и записывается в `Global.deletedObjects` (чтобы не появиться после загрузки).
- `ActivateOtherTrigger` включает/выключает другие триггеры по `Array<NodePath>`; `_on_body_entered(Node body)` — срабатывание при входе игрока.
- **Связь со сценами — через сигналы, подключённые в редакторе** (`[connection ...]` в `.tscn`): всего 474 подключения, из них 118 с `binds`. Самые частые: `body_entered → _on_body_entered` (63), `mouse_entered/exited` (53/53).
- Кинематики (`triggers/cinematic/`): движение камеры/персонажей по `Path`/`PathFollow`, `Cutscene`.

### Интерфейс

- `MenuManager` (static) — стек «одно открытое меню + фоновое»; меню реализуют `IMenu`.
- Меню: `MainMenu`, `PauseMenu`, `DeathMenu`, `SettingsMenu` (подменю `ControlsSubmenu`, `DifficultySubmenu`, `SettingsSubmenu`), `LoadMenu`, `SaveMenu`, `TestingLevelsMenu`.
- **Инвентарь**: `InventoryMenu` + режимы `InventoryMode` (`UsualMode`, `ChestMode`, `TradeMode`), обработчики `BindsHandler`, `UseHandler`; данные предметов — `ItemJSON` из `assets/lang/<lang>/items.json`.
- **Эффекты предметов** (`inventory/effects/`): `EffectHandler` + классы `Effect` (обычные C# классы, не узлы), пары «эффект/пост-эффект».
- **Диалоги** (`interface/Dialogue/`): `DialogueMenu` читает `assets/dialogues/<lang>/<npc>/<code>.json` (граф узлов `nodes` с `options`/`next`), запускает скрипты по имени через `Type.GetType("DialogueScripts." + name)` + `Activator.CreateInstance` (`DialogueScripts/*.cs`, интерфейс `IDialogueScript`). `Subtitles` работает аналогично для фоновых реплик. Озвучка — `DialogueAudio` + `assets/dialogues/configs`.
- **Локализация**: `InterfaceLang` (static) — `GetPhrase(file, section, phrase)` из `assets/lang/<lang>/<file>.json`; `LabelLang` для статических лейблов. Плейсхолдеры `#...#` заменяются на названия клавиш.

## 4. Сохранения

- Файл: `user://saves/<name>.sav`, открывается через `File.OpenCompressed` (сжатый формат Godot).
- Формат — построчный:
  1. имя сохранения
  2. дата
  3. номер уровня (`LevelsLoader.tempLevelNum`)
  4. `autosave_<slot>`
  5. раса
  6. `JSON.Print(Global.deletedObjects)` — пути удалённых узлов
  7. `JSON.Print(objectsData)` — словарь «путь узла → данные `GetSaveData()`»
- В сохранение попадают **все узлы группы `savable`**, реализующие `ISavable` (`GetSaveData()` / `LoadData(Dictionary)`) — ~60 классов.
- Объекты, созданные во время игры, называются `Created_*`; для них сохраняются `fileName` (путь к `.tscn`) и `parentPath`.
- Векторы сохраняются строкой (`Vector3.ToString()`) и парсятся `ConvertUtils.ParseToVector3`.
- Enum'ы кладутся прямо в `Godot.Collections.Dictionary` (например, `StateMachine`, `ItemJSON`) — в Godot 4 так нельзя без явного приведения.
- `SaveNode` в `Main` переносит инвентарь и «сохраняемые переменные» между уровнями.
- Настройки — отдельно, `ConfigFile` в `Settings.cs`.

## 5. Ассеты и сцены

- Типы узлов в сценах (топ): `CollisionShape` 1156, `MeshInstance` 1147, `Spatial` 921, `StaticBody` 392, `Label` 352, `Sprite3D` 168, `AudioStreamPlayer3D` 130, `Area` 128, `PhysicalBone` 97, `AnimationPlayer` 66, `OmniLight` 53, `Particles` 47, `GridMap` 45, `SpotLight` 35, `NavigationMeshInstance` 15, `WorldEnvironment` 12, `AnimationTree` 5, `Viewport` 5.
- Уровни очень большие (до 4.7 МБ текста) — меши часто встроены прямо в сцену (`ArrayMesh` sub_resource).
- Уровни собираются из **GridMap**-тайлов (`objects/props/tiles/*.tres|*.meshlib`, 5 бинарных `.meshlib`).
- Модели: 226 `.glb`, 51 `.fbx`, 7 `.obj`. При импорте материалы выносятся в файлы (`materials/storage=1`) → **679 бинарных `.material`**, на 388 из них сцены ссылаются напрямую.
- Прочие бинарные ресурсы: 41 `.res`, 7 `.mesh`, 26 `.anim`, 12 `.phymat`.
- Шейдеры: 3 файла `.shader` (`contrast`, `fisheye`, `godrays`) + встроенный в `assets/dialogues/blur.tres`.
- Шрифты: `DynamicFont` + `.ttf`.

## 6. Слои и ввод

Физические слои: 1 Default, 2 Ragdoll & Floor, 3 Interactive, 4 Exit, 5 Door, 6 Camera.
Слои рендера: 2 First Person, 3 Third Person.

Действия ввода (`project.godot`): `ui_*`, `jump`, `dash`, `crouch`, `changeView`, `legsHit`, `task`, `use`, `getGun`, `inventory`, `autoheal`, `ui_quicksave`, `ui_quickload`, `test_camera_*`. Переназначение клавиш — `ControlsSubmenu` (через `InputMap` и `Scancode`).

## 7. Соглашения кода

- Комментарии на русском, имена на английском, смешанный стиль: публичные поля часто `camelCase`, методы — `PascalCase`, обработчики сигналов из редактора — `_on_<node>_<signal>`.
- Узлы ищутся строковыми путями (`GetNode<T>("...")` — ~690 вызовов), почти нет `[Export] NodePath`.
- Асинхронность через `async void` + `await ToSignal(GetTree(), "idle_frame")` (46 мест) и `await global.ToTimer(...)`.
- Подключение сигналов в коде: `obj.Connect(nameof(Signal), this, nameof(Method))`.
- Интерфейсы поведения: `IInteractable`, `ISavable`, `IChest`, `IMenu`, `IActivated`, `IDoorTeleport`, `ITrader`.
- Отладка: `objects/testing/Testing.tscn` (F1 — лечение, F2 — убить врагов, F3 — убить игрока, тестовая камера), `TestingLevelsMenu` — быстрый выбор уровня.
