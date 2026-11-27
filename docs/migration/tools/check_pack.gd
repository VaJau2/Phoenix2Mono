extends SceneTree

# Этап 6: проверка полноты экспортированного пака. Официальные шаблоны экспорта не запускают
# внешние скрипты и сцены (-s, путь к сцене), поэтому пак открывается бинарником редактора:
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --main-pack "<exe-папка>/Phoenix 2 Mono.pck" \
#     -s <путь к этому файлу> -- --project=<корень проекта> > pack.log 2>&1
# Сверяет списки .tscn/.tres/.json проекта и пака (без docs/ и исключённых в export_presets.cfg),
# загружает каждую сцену/ресурс из пака (зависимости должны найтись) и разбирает каждый .json.
# C#-скрипты так не исполняются (сборки берутся из папки экспорта только самим шаблоном) — это проверка содержимого.

const EXCLUDED := ["res://docs/", "res://objects/props/PropDamage.tscn", "res://objects/props/buildings/CP.tscn",
	"res://objects/props/buildings/roof.tscn", "res://objects/props/training/wall-gun.tscn"]

var project := ""

func _init() -> void:
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--project="):
			project = a.substr(10).trim_suffix("/")
	_run.call_deferred()

func _run() -> void:
	var in_pack := {}
	_walk("res://", in_pack, true)
	var on_disk := {}
	_walk_disk(project, "res://", on_disk)
	var missing := 0
	for p in on_disk:
		if not in_pack.has(p):
			print("PACK MISSING ", p)
			missing += 1
	var failed := 0
	for p in in_pack:
		if p.ends_with(".json"):
			if JSON.parse_string(FileAccess.get_file_as_string(p)) == null:
				print("PACK BAD JSON ", p)
				failed += 1
		elif ResourceLoader.load(p) == null:
			print("PACK LOAD FAIL ", p)
			failed += 1
	print("PACK DONE files=%d disk=%d missing=%d load_failed=%d" % [in_pack.size(), on_disk.size(), missing, failed])
	quit()

func _want(p: String) -> bool:
	if not (p.ends_with(".tscn") or p.ends_with(".tres") or p.ends_with(".json")):
		return false
	for e in EXCLUDED:
		if p.begins_with(e):
			return false
	return true

func _walk(dir: String, out: Dictionary, _pack: bool) -> void:
	for d in DirAccess.get_directories_at(dir):
		if d.begins_with("."):
			continue
		_walk(dir.path_join(d), out, _pack)
	for f in DirAccess.get_files_at(dir):
		# в паке текстовые сцены/ресурсы лежат как .remap к бинарным в .godot/exported
		var p := dir.path_join(f).trim_suffix(".remap")
		if _want(p):
			out[p] = true

func _walk_disk(abs_dir: String, res_dir: String, out: Dictionary) -> void:
	for d in DirAccess.get_directories_at(abs_dir):
		if d.begins_with(".") or d == "bin" or d == "obj":
			continue
		_walk_disk(abs_dir.path_join(d), res_dir.path_join(d), out)
	for f in DirAccess.get_files_at(abs_dir):
		var p := res_dir.path_join(f)
		if _want(p):
			out[p] = true
