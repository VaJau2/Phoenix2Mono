extends SceneTree

# Этап 4 миграции: автоматический смоук-тест уровней в Godot 4.
#
# Запускает scenes/Main.tscn как в игре, по очереди загружает уровни через LevelsLoader.LoadLevel
# и даёт каждому поработать заданное число кадров, имитируя ввод игрока (ходьба, бег, прыжок,
# поворот камеры). Ошибки движка и исключения C# попадают в лог; перед ними стоят маркеры
# "SMOKE ..." — по ним видно, к какому уровню и шагу относится ошибка.
#
# Перед запуском: dotnet build "Phoenix 2 Mono.csproj"
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --fixed-fps 60 --path . \
#     -s <абсолютный путь к этому файлу> -- [--levels=1,2,5] [--frames=600] > smoke.log 2>&1
# --fixed-fps 60 — каждый кадр = 1/60 с игрового времени, независимо от скорости машины.

var levels: Array[int] = []
var frames := 600
var race := -1   # индекс расы в DEV-меню; -1 — по очереди для каждого уровня

func _init() -> void:
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--levels="):
			for s in a.substr(9).split(",", false):
				levels.append(s.to_int())
		elif a.begins_with("--frames="):
			frames = a.substr(9).to_int()
		elif a.begins_with("--race="):
			race = a.substr(7).to_int()
	if levels.is_empty():
		var json = JSON.parse_string(FileAccess.get_file_as_string("res://scenes/levels.json"))
		for k in json.keys():
			levels.append(int(k))
		levels.sort()
	_run.call_deferred()

func _run() -> void:
	var main: Node = load("res://scenes/Main.tscn").instantiate()
	main.name = "Main"
	root.add_child(main)
	_mark("MAIN loaded")
	await _wait(60)

	for level in levels:
		_mark("LEVEL %d start" % level)
		var old_children := main.get_children()
		_load_via_dev_menu(main, level)
		var scene: Node = null
		# уровень грузится в фоновом потоке, а кадры без окна идут без ограничения скорости,
		# поэтому ждём по реальному времени (до 3 минут)
		var deadline := Time.get_ticks_msec() + 180000
		while Time.get_ticks_msec() < deadline:
			await process_frame
			# новый уровень — новый дочерний узел Main из scenes/ (корень обычно "Scene", у титров — "Credits")
			for c in main.get_children():
				if not c in old_children and c.scene_file_path.begins_with("res://scenes/"):
					scene = c
			if scene != null:
				break
		if scene == null:
			_mark("LEVEL %d FAIL: scene not loaded" % level)
			continue
		_mark("LEVEL %d loaded: %s" % [level, scene.scene_file_path])
		await _wait(30)
		var player := scene.get_node_or_null("Player")
		_mark("LEVEL %d player: %s" % [level, player != null])
		await _play(level)
		_mark("LEVEL %d done" % level)

	_mark("ALL DONE")
	quit()

# Имитация игрока: ходьба вперёд, бег, поворот камеры, прыжок, шаг назад
func _play(level: int) -> void:
	var steps := [
		[0.15, ["ui_up"]],
		[0.15, ["ui_up", "ui_shift"]],
		[0.1, ["jump"]],
		[0.15, ["ui_left"]],
		[0.15, ["ui_down"]],
		[0.3, []],
	]
	for step in steps:
		var count := int(frames * step[0])
		_mark("LEVEL %d input %s" % [level, step[1]])
		for action in step[1]:
			Input.action_press(action)
		for i in count:
			if i % 20 == 0:
				var motion := InputEventMouseMotion.new()
				motion.relative = Vector2(15, 0)
				Input.parse_input_event(motion)
			await process_frame
		for action in step[1]:
			Input.action_release(action)

func _wait(n: int) -> void:
	for i in n:
		await process_frame

func _mark(text: String) -> void:
	printerr("SMOKE ", text)

# Загрузка уровня как из DEV-меню главного меню (TestingLevelsMenu._on_load_pressed):
# так задаются слот автосохранения и раса игрока, как при обычном запуске уровня
func _load_via_dev_menu(main: Node, level: int) -> void:
	var menu := main.get_node_or_null("Menu/MainMenu/Testing")
	if menu == null:
		_mark("LEVEL %d: DEV-меню не найдено (главное меню закрыто?), LoadLevel напрямую" % level)
		main.call("LoadLevel", level)
		return
	var levels_list: OptionButton = menu.get_node("levelsList")
	var race_list: OptionButton = menu.get_node("raceList")
	levels_list.select(level - 1)
	var r := race if race >= 0 else level % race_list.item_count
	race_list.select(r)
	_mark("LEVEL %d race: %s" % [level, race_list.get_item_text(r)])
	menu.call("_on_load_pressed")
