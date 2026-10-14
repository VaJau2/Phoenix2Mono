extends SceneTree

# Ищет свойства из текста .tscn/.tres, которых нет у объекта в Godot 4 — такие свойства
# движок при загрузке молча игнорирует (так терялись material/N, align у Button и т. п.).
#
# Узлы: сцена инстанцируется, свойство ищется в get_property_list() реального узла
# (учитываются экспорты C# и динамические свойства вроде surface_material_override/N).
# Ресурсы (sub_resource / gd_resource): свойство ищется в ClassDB по типу; свойства с "/"
# у ресурсов не проверяются (это динамические tracks/N/..., item/N/... и т. п.).
#
# Перед запуском: dotnet build "Phoenix 2 Mono.csproj"
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -s <абсолютный путь> -- --list=<файл со списком res://>
# Вывод: "MISSING <тип> <свойство> <файл>" и сводка по типам.

var summary := {}

func _init() -> void:
	var list_path := ""
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--list="):
			list_path = a.substr(7)
	var f := FileAccess.open(list_path, FileAccess.READ)
	while not f.eof_reached():
		var path := f.get_line().strip_edges()
		if path != "":
			_check_file(path)
	var keys := summary.keys()
	keys.sort()
	for k in keys:
		print("SUMMARY ", summary[k], "\t", k)
	quit()

func _check_file(path: String) -> void:
	var text := FileAccess.get_file_as_string(path)
	var scene_root: Node = null
	if path.ends_with(".tscn"):
		var packed = load(path)
		if packed is PackedScene:
			scene_root = packed.instantiate()

	var gd_type := ""
	var m := RegEx.create_from_string('^\\[gd_resource type="(\\w+)"').search(text)
	if m:
		gd_type = m.get_string(1)

	var section := ""        # "node" | "resource" | ""
	var res_type := ""
	var node: Node = null
	var node_props := {}
	var re_node := RegEx.create_from_string('^\\[node name="([^"]*)"(?: type="(\\w+)")?(?: parent="([^"]*)")?')
	var re_sub := RegEx.create_from_string('^\\[sub_resource type="(\\w+)"')
	var re_prop := RegEx.create_from_string('^([A-Za-z_][\\w/]*) = ')

	for line in text.split("\n"):
		if line.begins_with("["):
			section = ""
			var nm := re_node.search(line)
			if nm:
				section = "node"
				node = _find_node(scene_root, nm.get_string(1), nm.get_string(3))
				node_props = {}
				if node:
					for p in node.get_property_list():
						node_props[p.name] = true
				res_type = nm.get_string(2) if nm.get_string(2) != "" else ("?" + (node.get_class() if node else ""))
				continue
			var sm := re_sub.search(line)
			if sm:
				section = "resource"
				res_type = sm.get_string(1)
				continue
			if line == "[resource]" and gd_type != "":
				section = "resource"
				res_type = gd_type
			continue
		if section == "":
			continue
		var pm := re_prop.search(line)
		if not pm:
			continue
		var prop := pm.get_string(1)
		if section == "node":
			if node == null or prop in ["script", "groups"] or prop.begins_with("metadata/"):
				continue
			if not node_props.has(prop):
				_report(res_type, prop, path)
		else:
			if prop.contains("/") or prop in ["script", "resource_name", "resource_local_to_scene", "resource_path"]:
				continue
			if ClassDB.class_exists(res_type) and not _class_has(res_type, prop):
				_report(res_type, prop, path)

	if scene_root:
		scene_root.free()

func _class_has(cls: String, prop: String) -> bool:
	for p in ClassDB.class_get_property_list(cls):
		if p.name == prop:
			return true
	return false

func _find_node(root: Node, name: String, parent: String) -> Node:
	if root == null:
		return null
	if parent == "":
		return root
	var p := name if parent == "." else parent + "/" + name
	return root.get_node_or_null(NodePath(p))

func _report(type: String, prop: String, path: String) -> void:
	print("MISSING ", type, " ", prop, " ", path)
	var key := type + "." + prop
	summary[key] = summary.get(key, 0) + 1
