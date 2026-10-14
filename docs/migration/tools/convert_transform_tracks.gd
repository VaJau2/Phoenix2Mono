extends SceneTree

# Этап 3 миграции: перевод анимационных дорожек "transform" из Godot 3 в Godot 4.
#
# В Godot 4 нет типа дорожки "transform" — он разделён на position_3d / rotation_3d / scale_3d,
# и Godot 4.7 такие дорожки при загрузке просто отбрасывает (ошибки "track is out of bounds").
# Кроме того, в Godot 3 дорожка кости задавала позу ОТНОСИТЕЛЬНО rest кости (local = rest * pose),
# а в Godot 4 поза кости абсолютная. Поэтому значения для костей домножаются на rest.
#
# Скелеты Godot 3 не хранят позу костей (только rest); в Godot 4 без позы кость стоит в нулевом
# трансформе, поэтому скелетам дописывается поза = rest (bones/N/position, rotation, scale).
#
# Работает с текстом .tscn/.tres (Godot 4 не может загрузить такие анимации, чтобы их пересохранить).
# rest костей берутся из скелета в том же файле; для отдельных .tres анимаций — из --rest-scene.
#
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -s <абсолютный путь к этому файлу> -- \
#     --rest-scene=res://objects/characters/Player/Player.tscn <res:// файлы...>

var external_rests := {}   # имя кости -> Transform3D (для анимаций в отдельных файлах)
var stats := {"files": 0, "tracks": 0, "skeleton_poses": 0, "no_rest": 0}

func _init() -> void:
	var files := []
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--rest-scene="):
			external_rests = _collect_rests(_read(a.substr(13)))
		elif a.begins_with("res://"):
			files.append(a)

	for path in files:
		var text := _read(path)
		var converted := _convert_text(text, path)
		if converted != text:
			var f := FileAccess.open(path, FileAccess.WRITE)
			f.store_string(converted)
			f.close()
			stats.files += 1

	print("DONE ", stats)
	quit()

# --- скелеты ---------------------------------------------------------------

# skeleton name -> { bone name -> rest }; если имя скелета не важно — ключ "*"
func _collect_skeletons(lines: PackedStringArray) -> Dictionary:
	var result := {}
	var current := ""
	var names := {}
	for line in lines:
		if line.begins_with("["):
			current = ""
			if line.begins_with("[node ") and line.contains('type="Skeleton3D"'):
				current = _attr(line, "name")
				result[current] = {}
				names = {}
			continue
		if current == "":
			continue
		var m := _match("^bones/(\\d+)/name = \"(.*)\"$", line)
		if m:
			names[m.get_string(1)] = m.get_string(2)
			continue
		m = _match("^bones/(\\d+)/rest = (Transform3D\\(.*\\))$", line)
		if m and names.has(m.get_string(1)):
			result[current][names[m.get_string(1)]] = str_to_var(m.get_string(2))
	return result

func _collect_rests(text: String) -> Dictionary:
	var skeletons := _collect_skeletons(text.split("\n"))
	var rests := {}
	for s in skeletons.values():
		rests.merge(s)
	return rests

# Дописывает bones/N/position|rotation|scale = rest, если позы нет
func _add_skeleton_poses(lines: PackedStringArray) -> PackedStringArray:
	var out := PackedStringArray()
	var in_skeleton := false
	var has_pose := {}
	for line in lines:
		if line.begins_with("["):
			in_skeleton = line.begins_with("[node ") and line.contains('type="Skeleton3D"')
			has_pose = {}
		if in_skeleton:
			var p := _match("^bones/(\\d+)/(position|rotation|scale) = ", line)
			if p:
				has_pose[p.get_string(1)] = true
		out.append(line)
		if not in_skeleton:
			continue
		var m := _match("^bones/(\\d+)/rest = (Transform3D\\(.*\\))$", line)
		if m and not has_pose.has(m.get_string(1)):
			var rest: Transform3D = str_to_var(m.get_string(2))
			var i := m.get_string(1)
			out.append("bones/%s/position = %s" % [i, var_to_str(rest.origin)])
			out.append("bones/%s/rotation = %s" % [i, var_to_str(rest.basis.get_rotation_quaternion())])
			out.append("bones/%s/scale = %s" % [i, var_to_str(rest.basis.get_scale())])
			stats.skeleton_poses += 1
	return out

# --- анимации --------------------------------------------------------------

func _convert_text(text: String, path: String) -> String:
	if not text.contains('type = "transform"'):
		return text
	var lines := text.split("\n")
	var skeletons := _collect_skeletons(lines)
	lines = _add_skeleton_poses(lines)

	var out := PackedStringArray()
	var block := PackedStringArray()
	var in_anim := false
	for line in lines:
		if line.begins_with("["):
			if in_anim:
				out.append_array(_convert_animation(block, skeletons, path))
				block = PackedStringArray()
			in_anim = false
			out.append(line)
			# Animation: [sub_resource type="Animation" ...] или [resource] в .tres c gd_resource type="Animation"
			if line.begins_with('[sub_resource type="Animation"') or (line == "[resource]" and text.begins_with('[gd_resource type="Animation"')):
				in_anim = true
			continue
		if in_anim:
			block.append(line)
		else:
			out.append(line)
	if in_anim:
		out.append_array(_convert_animation(block, skeletons, path))
	return "\n".join(out)

func _convert_animation(block: PackedStringArray, skeletons: Dictionary, path: String) -> PackedStringArray:
	if not '\n'.join(block).contains('type = "transform"'):
		return block

	var head := PackedStringArray()   # строки до первой дорожки
	var tracks := {}                  # old index -> [[prop, value], ...]
	var order := []
	var last_prop = null              # значения value-дорожек занимают несколько строк (keys = {...})
	for line in block:
		var m := _match("^tracks/(\\d+)/([\\w/]+) = (.*)$", line)
		if m:
			var idx := m.get_string(1).to_int()
			if not tracks.has(idx):
				tracks[idx] = []
				order.append(idx)
			last_prop = [m.get_string(2), m.get_string(3)]
			tracks[idx].append(last_prop)
		elif last_prop == null:
			head.append(line)
		else:
			last_prop[1] += "\n" + line
	for idx in order:
		for p in tracks[idx]:
			p[1] = p[1].rstrip("\n ")

	var out := head.duplicate()
	var new_idx := 0
	for idx in order:
		var props: Array = tracks[idx]
		if _prop(props, "type") != '"transform"':
			for p in props:
				out.append("tracks/%d/%s = %s" % [new_idx, p[0], p[1]])
			new_idx += 1
			continue
		for t in _split_transform_track(props, skeletons, path):
			for p in t:
				out.append("tracks/%d/%s = %s" % [new_idx, p[0], p[1]])
			new_idx += 1
		stats.tracks += 1
	if block.size() > 0 and block[block.size() - 1] == "":
		out.append("")
	return out

# Godot 3: ключ = [time, transition, px, py, pz, qx, qy, qz, qw, sx, sy, sz]
func _split_transform_track(props: Array, skeletons: Dictionary, path: String) -> Array:
	var node_path: String = _prop(props, "path")
	var inner := node_path.trim_prefix('NodePath("').trim_suffix('")')
	var rest := Transform3D.IDENTITY
	var colon := inner.find(":")
	if colon >= 0:
		var skeleton_name := inner.substr(0, colon).get_file()
		var bone := inner.substr(colon + 1)
		var rests: Dictionary = skeletons.get(skeleton_name, {})
		if rests.is_empty() and skeletons.size() == 1:
			rests = skeletons.values()[0]
		if rests.is_empty():
			rests = external_rests
		if rests.has(bone):
			rest = rests[bone]
		else:
			stats.no_rest += 1
			printerr("WARN no rest for ", inner, " in ", path)

	var keys_text: String = _prop(props, "keys")
	var nums := keys_text.trim_prefix("PackedFloat32Array(").trim_suffix(")").split(",", false)
	var pos := PackedStringArray()
	var rot := PackedStringArray()
	var scl := PackedStringArray()
	for k in range(0, nums.size() - 11, 12):
		var time := nums[k].strip_edges()
		var trans := nums[k + 1].strip_edges()
		var p := Vector3(nums[k + 2].to_float(), nums[k + 3].to_float(), nums[k + 4].to_float())
		var q := Quaternion(nums[k + 5].to_float(), nums[k + 6].to_float(), nums[k + 7].to_float(), nums[k + 8].to_float())
		var s := Vector3(nums[k + 9].to_float(), nums[k + 10].to_float(), nums[k + 11].to_float())
		var pose := Transform3D(Basis(q.normalized()) * Basis.from_scale(s), p)
		var local := rest * pose
		var lq := local.basis.get_rotation_quaternion()
		var ls := local.basis.get_scale()
		pos.append_array([time, trans, _f(local.origin.x), _f(local.origin.y), _f(local.origin.z)])
		rot.append_array([time, trans, _f(lq.x), _f(lq.y), _f(lq.z), _f(lq.w)])
		scl.append_array([time, trans, _f(ls.x), _f(ls.y), _f(ls.z)])

	var common := []
	for name in ["imported", "enabled", "path", "interp", "loop_wrap"]:
		var v = _prop(props, name)
		if v != null:
			common.append([name, v])
	return [
		[["type", '"position_3d"']] + common + [["keys", "PackedFloat32Array(%s)" % ", ".join(pos)]],
		[["type", '"rotation_3d"']] + common + [["keys", "PackedFloat32Array(%s)" % ", ".join(rot)]],
		[["type", '"scale_3d"']] + common + [["keys", "PackedFloat32Array(%s)" % ", ".join(scl)]],
	]

# --- утилиты ---------------------------------------------------------------

func _prop(props: Array, name: String):
	for p in props:
		if p[0] == name:
			return p[1]
	return null

func _f(x: float) -> String:
	return String.num(x, 6)

func _attr(line: String, name: String) -> String:
	var m := _match(name + "=\"([^\"]*)\"", line)
	return m.get_string(1) if m else ""

func _match(pattern: String, s: String) -> RegExMatch:
	var re := RegEx.new()
	re.compile(pattern)
	return re.search(s)

func _read(path: String) -> String:
	var f := FileAccess.open(path, FileAccess.READ)
	var t := f.get_as_text()
	f.close()
	return t
