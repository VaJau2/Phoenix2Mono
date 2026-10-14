extends SceneTree

# Godot 3: загружает все ресурсы из списка и сообщает, какие не загрузились.
# Ошибки движка (битые зависимости и т. п.) пишутся в stderr — смотреть лог целиком.
#
# Godot_v3.6.3-stable_mono_win64.exe --no-window --path . -s docs/migration/tools/load_all.gd --list=<файл со списком res:// путей>

func _init():
	var list_path = ""
	for a in OS.get_cmdline_args():
		if a.begins_with("--list="):
			list_path = a.substr(7)

	var f = File.new()
	if f.open(list_path, File.READ) != OK:
		printerr("cannot open list ", list_path)
		quit(1)
		return

	var total = 0
	var failed = 0
	while not f.eof_reached():
		var path = f.get_line().strip_edges()
		if path == "":
			continue
		total += 1
		print("LOAD ", path)
		var res = ResourceLoader.load(path, "", true)
		if res == null:
			failed += 1
			print("FAIL ", path)
	f.close()

	print("DONE total=", total, " failed=", failed)
	quit()
