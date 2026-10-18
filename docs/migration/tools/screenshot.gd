extends SceneTree

# Этап 5 миграции: скриншоты уровней в Godot 4 для сравнения с эталоном Godot 3.
#
# Запускает scenes/Main.tscn с окном (НЕ --headless — нужен рендер), загружает уровни через
# DEV-меню и сохраняет кадр с точки появления игрока (вид от первого лица) в --out.
# Размер окна берётся из settings.cfg — эталонные скриншоты сняты в том же окне.
#
# Godot_v4.7.2-stable_mono_win64_console.exe --path . -s <абсолютный путь к этому файлу> \
#     -- [--levels=1,2] [--out=<папка>] [--wait=4] [--race=0] [--suffix=_v2] [--third] > shots.log 2>&1
# --wait — секунд после загрузки уровня до снимка (частицы, туман, автоэкспозиция).
# --third — дополнительно снимок от третьего лица (lvl<N><suffix>_third.png), виден игрок.

var levels: Array[int] = []
var out_dir := "user://screenshots"
var wait_sec := 4.0
var race := 0
var suffix := ""
var third := false

func _init() -> void:
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--levels="):
			for s in a.substr(9).split(",", false):
				levels.append(s.to_int())
		elif a.begins_with("--out="):
			out_dir = a.substr(6)
		elif a.begins_with("--wait="):
			wait_sec = a.substr(7).to_float()
		elif a.begins_with("--race="):
			race = a.substr(7).to_int()
		elif a.begins_with("--suffix="):
			suffix = a.substr(9)
		elif a == "--third":
			third = true
	if levels.is_empty():
		var json = JSON.parse_string(FileAccess.get_file_as_string("res://scenes/levels.json"))
		for k in json.keys():
			levels.append(int(k))
		levels.sort()
	DirAccess.make_dir_recursive_absolute(out_dir)
	_run.call_deferred()

func _run() -> void:
	var main: Node = load("res://scenes/Main.tscn").instantiate()
	main.name = "Main"
	root.add_child(main)
	await _wait(1.0)
	for level in levels:
		var old_children := main.get_children()
		var menu := main.get_node_or_null("Menu/MainMenu/Testing")
		if menu:
			menu.get_node("levelsList").select(level - 1)
			menu.get_node("raceList").select(race)
			menu.call("_on_load_pressed")
		else:
			main.call("LoadLevel", level)
		var scene: Node = null
		var deadline := Time.get_ticks_msec() + 180000
		while Time.get_ticks_msec() < deadline and scene == null:
			await process_frame
			for c in main.get_children():
				if not c in old_children and c.scene_file_path.begins_with("res://scenes/"):
					scene = c
		if scene == null:
			printerr("SHOT level %d: не загрузился" % level)
			continue
		await _wait(wait_sec)
		await _save("lvl%d%s.png" % [level, suffix], level)
		if third:
			# переключение вида — как клавиша F у игрока (игра ловит событие в _Input)
			await _action("changeView")
			await _wait(1.0)
			await _save("lvl%d%s_third.png" % [level, suffix], level)
			await _action("changeView")
	quit()

func _action(action: String) -> void:
	for pressed in [true, false]:
		var ev := InputEventAction.new()
		ev.action = action
		ev.pressed = pressed
		Input.parse_input_event(ev)
		await _wait(0.1)

func _save(file: String, level: int) -> void:
	await RenderingServer.frame_post_draw
	var img := root.get_texture().get_image()
	var path := out_dir.path_join(file)
	img.save_png(path)
	printerr("SHOT level %d -> %s" % [level, ProjectSettings.globalize_path(path)])

func _wait(sec: float) -> void:
	var end := Time.get_ticks_msec() + int(sec * 1000)
	while Time.get_ticks_msec() < end:
		await process_frame
