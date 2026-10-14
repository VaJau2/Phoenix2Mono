# Инвентаризация: что затронет переход Godot 3.6 → 4.x

> Снимок на 2026-10-14 (ветка `PHNX-83`). Счётчики C# можно пересчитать: `bash docs/migration/inventory.sh`.
> Общее описание проекта — [../architecture.md](../architecture.md).

Уровень риска: 🔴 высокий (ломает сборку/загрузку или требует ручной работы во многих местах), 🟡 средний, 🟢 низкий (механическая замена).

## 1. Проект и сборка

| Что | Сейчас | Godot 4 | Риск |
|---|---|---|---|
| SDK | `Godot.NET.Sdk/3.3.0`, `net472` | `Godot.NET.Sdk/4.x`, `net8.0` (минимум для 4.4+) | 🟢 |
| Секция настроек | `[mono]` | `[dotnet]` | 🟢 конвертер |
| `config_version` | 4 | 5 | 🟢 конвертер |
| Ввод в `project.godot` | `scancode` | `keycode` (коды спецклавиш другие: `16777218` → `4194306`) | 🟢 конвертер |
| `window/stretch/mode="2d"` | | `canvas_items` | 🟢 конвертер |
| `.import` / `/.import/` | | `.godot/imported/`, все ассеты реимпортируются | 🟡 долго, `.import` файлы перепишутся |
| Ресурсы `.uid` | нет | Godot 4.4+ создаёт `*.uid` рядом со скриптами/шейдерами — их нужно коммитить | 🟢 |

## 2. C# код

Godot 4 полностью поменял C# API: проект **не скомпилируется**, пока не исправлены все пункты ниже. Конвертер Godot частично переименовывает классы в `.cs`, но большую часть придётся править вручную/агентом по ошибкам `dotnet build`.

### 2.1 Счётчики (по `inventory.sh`)

| Паттерн | Файлов | Вхождений | Что делать | Риск |
|---|---:|---:|---|---|
| Классы-наследники Godot без `partial` | ~267 | 267 | Все классы, наследующие `GodotObject`, должны быть `partial` (source generators) | 🟢 механически |
| `[Signal] delegate` без суффикса `EventHandler` | 30 | 46 | `delegate void TakenDamage()` → `delegate void TakenDamageEventHandler()`; ссылки `nameof(Character.TakenDamage)` → `SignalName.TakenDamage` | 🟡 |
| `Connect(sig, this, method)` (3.x) | 12 | 24 | → `Connect(SignalName.X, new Callable(this, MethodName.Y))` или C#-события `obj.X += Y` | 🟡 |
| `EmitSignal(nameof(...))` | 37 | 54 | → `EmitSignal(SignalName.X, ...)`; аргументы должны быть Variant-совместимы | 🟡 |
| `ToSignal(GetTree(), "idle_frame")` | 35 | 46 | → `SceneTree.SignalName.ProcessFrame` | 🟢 |
| `PackedScene.Instance()` | 20 | 32 | → `Instantiate()` / `Instantiate<T>()` | 🟢 |
| `Spatial` | 75 | 221 | → `Node3D` | 🟢 конвертер |
| 3D-узлы без суффикса `3D` | 71 | 276 | `Area`→`Area3D`, `MeshInstance`→`MeshInstance3D`, `Camera`→`Camera3D`, `Skeleton`→`Skeleton3D`, `Particles`→`GPUParticles3D`, `Listener`→`AudioListener3D`, `Position3D`→`Marker3D`... | 🟢 конвертер |
| `Transform` | 17 | 66 | → `Transform3D` | 🟢 |
| `GlobalTranslation` / `Translation` | 45 | 111 | → `GlobalPosition` / `Position` | 🟢 |
| `.x/.y/.z/.origin/.basis` | 71 | 453 | → `.X/.Y/.Z/.Origin/.Basis` | 🟢 механически |
| `_Process(float delta)` | 102 | 103 | → `double delta` (+ приведения `(float)delta` в формулах) | 🟢 |
| `KinematicBody` + `MoveAndSlide(vel, up, ...)` | 4 | 4 | `CharacterBody3D`: `Velocity`, `UpDirection`, `FloorStopOnSlope`, `MaxSlides` — свойства; `MoveAndSlide()` без аргументов. **Конфликт: в `Character` уже есть поле `Velocity`** | 🔴 влияет на всё движение |
| `File` / `Directory` | 5 | 16 | → `FileAccess.Open/OpenCompressed`, `DirAccess` (статические фабрики) | 🟡 |
| `JSON.Parse` / `JSON.Print` | 4 | 6 | → `Json.ParseString` / `Json.Stringify` | 🟡 сохранения |
| `ResourceInteractiveLoader` | 1 | 2 | → `ResourceLoader.LoadThreadedRequest` + `LoadThreadedGetStatus` (`LevelsLoader`) | 🟡 |
| `PauseMode` | 3 | 9 | → `ProcessMode` (`Process` → `Always`) | 🟢 |
| `Scancode` / `ButtonList` / `KeyList` | 8 | 59 | → `Keycode`, `MouseButton`, `Key`; `OS.GetScancodeString` → `OS.GetKeycodeString` | 🟡 переназначение клавиш, терминалы |
| `Rect*` у `Control` | 11 | 46 | `RectSize` → `Size`, `RectPosition` → `Position`, `RectMinSize` → `CustomMinimumSize` | 🟢 |
| `Godot.Object` | 8 | 8 | → `GodotObject` | 🟢 |
| `SpatialMaterial` / `SetShaderParam` | 14 | 32 | → `StandardMaterial3D` / `SetShaderParameter`; часть свойств материала переименована | 🟡 |
| `Dictionary.Contains("key")` | 53 | 108 | → `ContainsKey` | 🟢 |
| `BeginsWith` / `Empty()` | 7 | 10 | → `StartsWith` / `IsEmpty()` | 🟢 |

### 2.2 Изменения, которые не ловятся простым поиском (🔴/🟡)

1. **Variant вместо `object` в коллекциях Godot.** В Godot 4 `Godot.Collections.Dictionary/Array` хранят `Variant`, и неявного приведения из `Variant` к `Dictionary`/`int`/`string` нет. Затронуто:
   - касты `(Dictionary)x`, `x as Dictionary`, `x is Dictionary d` — ~50 мест в 25 файлах → `.AsGodotDictionary()`, `.AsInt32()` и т. п.;
   - `Convert.ToInt32(data["..."])`, `data["..."].ToString()` — ~240 мест (в основном `LoadData` всех `ISavable`);
   - **enum'ы, положенные в Dictionary**: `StateMachine.GetSaveData` (`saveData["state"] = ...`), `ItemJSON.ConvertItemType` (`itemData["type"] = itemType`) — нужно явно `(int)` или `Variant.From`, а при чтении — обратно;
   - `NpcStateDependencies` + `DependencyInjection`: DI-контейнер — это `Godot.Collections.Dictionary` с узлами; узлы в Variant помещаются, но логика каста при вызове конструктора (`parameters[i] = classes[...]`) отдаст `Variant`, а не объект → нужен `.AsGodotObject()` или переход на `System.Collections.Generic.Dictionary<string, object>`.
2. **Аргументы сигналов должны быть Variant-совместимыми.** Не скомпилируются/не будут работать сигналы с аргументами-интерфейсами или не-Godot типами:
   - `Character.DoorTeleporting(Spatial)` эмитится с `IDoorTeleport` (интерфейс);
   - `RadioBase.ChangeOnline(RadioBase)`, `DragonSpawnTrigger.Spawned(NPC)` — ок (узлы), но проверить;
   - сигналы с параметрами по умолчанию (`Receiver.ChangeMusicEvent(AudioStream stream = null)`) — проверить вызовы без аргумента.
3. **Методы, вызываемые движком по имени** (из `[connection]` в сценах, `Call`, `CallDeferred`, `Connect`): в Godot 4 source generator регистрирует только методы с Variant-совместимыми параметрами. Обработчики `_on_*` с параметром `Node body` подходят, но методы с параметрами-интерфейсами или C# классами — нет. 474 подключения в сценах, 118 из них с `binds` — при несовпадении количества аргументов в Godot 4 будут ошибки в рантайме, не при сборке.
4. **`[Export]`**: все 402 экспорта должны быть Variant-типов. Проверить `[Export] Array<SetStateEnum>` / `Array<NpcStateEnum>` (`StateMachine`) и `[Export] Dictionary<string, int>` (`PlayerSpawner`) — значения, заданные в инспекторе в сценах, могут не перенестись.
5. **Сохранение NPC с динамическими сигналами** (`NPC.GetSaveData/LoadData`, `GetSignalConnectionList`): формат списка подключений в Godot 4 другой (`Callable` вместо `target`+`method`). В старой ветке `PHNX-154` это уже переписывалось — коммит `30617142`.
6. **PhysicalBone / рэгдоллы** (`NpcSkeleton`, `PlayerDead`, `PlayerBody`, 97 узлов `PhysicalBone` в сценах): в Godot 4.3+ физические кости — дети узла `PhysicalBoneSimulator3D`, `Skeleton3D.PhysicalBonesStartSimulation` устарел. Конвертер структуру сцен не перестраивает.
7. **AnimationTree** (`PlayerBody`, `PonyBody`, `ChangePlayerLookTrigger`, 5 сцен): параметры `"parameters/..."` в целом совместимы, но `AnimationTree` в 4.x — наследник `AnimationMixer`, требует `AnimPlayer` и `Active`; переходы стейт-машин (`AnimationNodeStateMachineTransition`, 88 шт.) получили новые свойства (`advance_mode`, `switch_mode`).
8. **Навигация**: `NavigationServer.MapGetPath(GetWorld().NavigationMap, ...)` → `NavigationServer3D.MapGetPath(GetWorld3D().NavigationMap, ...)`. 15 `NavigationMeshInstance` → `NavigationRegion3D`; параметры навмеша (cell size/height) должны совпадать с настройками карты — скорее всего навмеши придётся перепечь.
9. **`async void` + `await ToSignal`** (75 `async void` в 57 файлах): поведение то же, но при освобождении узла во время `await` в Godot 4 будет исключение `ObjectDisposedException` вместо тихой ошибки — проверить после запуска.
10. **Строковый формат `Vector3.ToString()`** используется в сохранениях (`ConvertUtils.ParseToVector3`). Нужно проверить, что формат и культура (ru_RU: запятая как десятичный разделитель!) совпадают в 3 и 4 — иначе старые сохранения не загрузятся.
11. **`Engine.TimeScale`, `AudioServer`, `OS.Window*`** (`Settings.cs`): полноэкранный режим/vsync → `DisplayServer.WindowSetMode`, `DisplayServer.WindowSetVsyncMode`.
12. **`Listener`** → `AudioListener3D` (`Player.cs`, `Cutscene.cs`).

## 3. Ассеты и сцены

| Что | Кол-во | Что меняется в Godot 4 | Риск |
|---|---:|---|---|
| **Бинарные `.material`** (SpatialMaterial) | 679 (388 используются сценами напрямую, 615 ссылок) | Конвертер проекта **не обрабатывает бинарные ресурсы** — только текстовые. Нужно заранее (в Godot 3) перевести в `.tres` или пересоздать | 🔴 |
| Прочие бинарные ресурсы: `.res` 41, `.mesh` 7, `.meshlib` 5, `.anim` 26, `.phymat` 12 | 91 | То же. `.meshlib` — библиотеки тайлов GridMap для уровней, `.anim` — анимации игрока, `.res` в `assets/models/characters/ponies/player` и `player_variants` | 🔴 |
| Встроенные `ArrayMesh` в `.tscn`/`.tres` | 852 | Формат меша в 4.x другой; Godot 4.2+ умеет апгрейдить (`Project → Tools → Upgrade Mesh Surfaces`) | 🟡 |
| `Skin` | 80 | Привязка мешей к скелету — проверить персонажей визуально | 🟡 |
| Импорт `.glb` (226) / `.fbx` (51) / `.obj` (7) | 284 | Полный реимпорт; в 4.x другая структура импортированных сцен и материалов. Старая ветка PHNX-154 большую часть времени ушла именно на реимпорт моделей | 🔴 визуал |
| `SpatialMaterial` встроенные | 214 | → `StandardMaterial3D` (конвертер); прозрачность, unshaded, vertex color и т. п. переименованы | 🟡 |
| `ParticlesMaterial` / `Particles` | 50 / 47 | → `ParticleProcessMaterial` / `GPUParticles3D` | 🟡 |
| `Environment` + `ProceduralSky` / `PanoramaSky` | 11 + 10 | → `Sky` + `ProceduralSkyMaterial` / `PanoramaSkyMaterial`. Тонмаппинг, glow, fog в 4.x работают иначе — **освещение уровней будет выглядеть по-другому** | 🔴 визуал |
| Источники света (Omni/Spot/Directional) | 105 | Другая модель затухания (`attenuation`), энергии; тени | 🟡 визуал |
| `GridMap` | 45 | Данные ячеек конвертируются; зависят от `.meshlib` | 🟡 |
| `PhysicalBone` | 97 | См. п. 2.2.6 | 🔴 |
| `NavigationMeshInstance` / `NavigationMesh` | 15 / 15 | → `NavigationRegion3D`, перепечь | 🟡 |
| `AnimationTree` / `AnimationNodeStateMachine*` | 5 / ~150 | См. п. 2.2.7 | 🟡 |
| `Viewport` в сценах | 5 | → `SubViewport` (+ `SubViewportContainer`), меняется `render_target_*` | 🟡 (зеркало `Mirror`, лица NPC?) |
| `DynamicFont` + `.ttf` | 20 + 5 | → `FontFile` / `FontVariation`, размер шрифта — в теме/`theme_override_font_sizes` | 🟡 UI |
| `Theme` / `StyleBoxFlat` | 7 / 41 | Конвертер переносит, отступы/размеры могут поплыть | 🟡 UI |
| Шейдеры `.shader` | 3 + 1 встроенный | → `.gdshader`; `hint_color` → `source_color`, `SCREEN_TEXTURE` → `uniform sampler2D ... : hint_screen_texture`, `WORLD_MATRIX` → `MODEL_MATRIX` | 🟡 |
| Встроенные GDScript в сценах | ~35 в 26 сценах | Конвертер обрабатывает GDScript (`onready` → `@onready` и т. п.) | 🟢 |
| Битые ссылки на `.gd` | 3 файла | `CP.tscn`, `window.tscn` → `res://assets/scripts/furns/broken-glass.gd`, `climbing.gd`; `flyingRing.tscn` → `res://assets/scripts/training/flyingRing.gd` — файлов нет уже в Godot 3 | 🟢 почистить заранее |
| Сцены > 4 МБ | 3 (`AfterwarBelllgatesBase` 4.7 МБ, `SnowBase` 4.5, `BellgatesBase` 4.2) | У конвертера есть лимит размера файла (по умолчанию ~4 МБ) — большие файлы могут быть **молча пропущены**. Запускать конвертер из CLI с увеличенным лимитом | 🔴 |

## 4. Старая ветка `origin/PHNX-154-godot-4-update`

- 13 коммитов, март–апрель 2024, Godot 4.2.1, ~3100 изменённых файлов; ответвлена от `main` до 50 последних коммитов текущей ветки.
- Первый коммит: «игра билдится, но главное меню не открывается» — компиляцию тогда довели до конца; дальше основное время ушло на ассеты (реимпорт машин, моделей улицы, тела игрока).
- **Не мержить**, но использовать как справочник решений:
  ```bash
  git show origin/PHNX-154-godot-4-update:assets/scripts/characters/NPC/NPC.cs
  git diff origin/main...origin/PHNX-154-godot-4-update -- assets/scripts/<путь>
  ```
- Изменённые C# файлы: 249 из 331 — хорошая подсказка, как уже переводились конкретные классы.

## 5. Принятые решения

Подробности и порядок работ — в [plan.md](plan.md).

1. **Сохранения**: старые сохранения (Godot 3) должны загружаться — формат сохраняем, проверяем на реальных файлах.
2. **Рендерер**: Forward+. Точное повторение старой картинки не требуется, материалы должны выглядеть похоже.
3. **Физика**: Jolt, если на практике он не хуже GodotPhysics (решается одной настройкой проекта).
4. `.fbx`: пока импорт встроенным ufbx; переконвертация в `.glb` — только для моделей с проблемами.

## 6. Бинарные ресурсы — проверка автоматической конвертации

Проверено 2026-10-14: скрипт на GDScript, запущенный в Godot 3.6.3 (`--no-window -s`), загружает и пересохраняет в `.tres` все шесть типов (`.material`, `.meshlib`, `.anim`, `.res`, `.mesh`, `.phymat`); внешние ссылки (текстуры, материалы) сохраняются как `ext_resource`.

Нюансы:
- 3 конфликта имён с уже существующими `.tres`: `assets/models/characters/ponies/scientist3/wing`, `assets/models/props/buildings/base-houses/stolovaya/walls`, `assets/models/props/buildings/street-houses/market/inside/market`.
- Пути к бинарным ресурсам собираются в C# коде динамически: `Player.cs` (`mesh.res`, `skin.res`, `.res`), `PlayerDead.cs`, `NpcFaceMat.cs` (`/0.material`, `/1.material`), `RoboEyeBody.cs`, `StealthBoyEffect.cs`.
- Суммарный объём бинарных файлов ~28 МБ; в текстовом виде меши (`.res`, `.meshlib`) увеличатся в несколько раз.
