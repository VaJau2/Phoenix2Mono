extends SceneTree

# Godot 4: загружает и инстанцирует все сцены/ресурсы из списка, чтобы собрать ошибки загрузки.
# При инстанцировании Godot проверяет подключения сигналов из сцен ([connection]) — ошибки
# "Cannot connect to ..." тоже попадают в лог. _ready() не вызывается (узлы не добавляются в дерево).
#
# Перед запуском собрать C#: dotnet build "Phoenix 2 Mono.csproj"
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -s <абсолютный путь к этому файлу> -- --list=<файл со списком res:// путей>
# В логе перед ошибками каждой сцены стоит строка "LOAD <путь>".

func _init() -> void:
	var list_path := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--list="):
			list_path = a.substr(7)

	var f := FileAccess.open(list_path, FileAccess.READ)
	if f == null:
		printerr("cannot open list ", list_path)
		quit(1)
		return

	var total := 0
	var failed := 0
	while not f.eof_reached():
		var path := f.get_line().strip_edges()
		if path == "":
			continue
		total += 1
		printerr("LOAD ", path)
		var res = ResourceLoader.load(path, "", ResourceLoader.CACHE_MODE_REUSE)
		if res == null:
			failed += 1
			print("FAIL ", path)
			continue
		if res is PackedScene:
			var node: Node = res.instantiate()
			if node == null:
				failed += 1
				print("FAIL instantiate ", path)
				continue
			node.free()

	print("DONE total=", total, " failed=", failed)
	quit()
