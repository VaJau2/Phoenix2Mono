extends SceneTree

# Этап 6: замер производительности уровня (Godot 4).
# Загружает уровень через DEV-меню (земная пони), ждёт прогрева, затем меряет время кадра,
# пока игрок стоит на точке появления. Только с окном (без --headless), vsync выключен.
# "<godot или exe>" --resolution 1280x720 [--path .] -s <путь к этому файлу> -- --level=N [--warmup=300] [--frames=600] >> perf.log
# Вывод — строка "PERF level=N load_ms=… avg_fps=… p50_ms=… p99_ms=… draw_calls=…".
# Шаблоны экспорта внешние скрипты не запускают — мерить бинарником редактора (--path .), он чуть медленнее release-сборки.

var level := 1
var warmup := 300
var frames := 600

func _init() -> void:
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--level="):
			level = a.substr(8).to_int()
		elif a.begins_with("--warmup="):
			warmup = a.substr(9).to_int()
		elif a.begins_with("--frames="):
			frames = a.substr(9).to_int()
	_run.call_deferred()

func _run() -> void:
	DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
	Engine.max_fps = 0
	var main: Node = load("res://scenes/Main.tscn").instantiate()
	main.name = "Main"
	root.add_child(main)
	for i in 60:
		await process_frame
	var menu := main.get_node("Menu/MainMenu/Testing")
	(menu.get_node("levelsList") as OptionButton).select(level - 1)
	(menu.get_node("raceList") as OptionButton).select(0)
	var old := main.get_children()
	var t0 := Time.get_ticks_msec()
	menu.call("_on_load_pressed")
	var scene: Node = null
	while scene == null and Time.get_ticks_msec() - t0 < 180000:
		await process_frame
		for c in main.get_children():
			if not c in old and c.scene_file_path.begins_with("res://scenes/"):
				scene = c
	if scene == null:
		print("PERF level=%d FAIL" % level)
		quit()
		return
	var load_ms := Time.get_ticks_msec() - t0
	for i in warmup:
		await process_frame
	var times: Array[float] = []
	var draws := 0.0
	var prev := Time.get_ticks_usec()
	for i in frames:
		await process_frame
		var now := Time.get_ticks_usec()
		times.append((now - prev) / 1000.0)
		prev = now
		draws += Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME)
	var total := 0.0
	for t in times:
		total += t
	times.sort()
	print("PERF level=%d load_ms=%d avg_fps=%.1f p50_ms=%.2f p99_ms=%.2f draw_calls=%d" % [
		level, load_ms, frames * 1000.0 / total, times[frames / 2], times[int(frames * 0.99)], int(draws / frames)])
	quit()
