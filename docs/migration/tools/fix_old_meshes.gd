extends SceneTree

# Этап 5 миграции: нормали мешей, сохранённых в формате Godot 3 (B-18).
#
# Меши, встроенные в сцены/ресурсы из Godot 3 ("surfaces/N = { "array_data": ... }"), при загрузке
# в Godot 4 переводятся в новый формат, и сжатые нормали/касательные Godot 3 декодируются неверно:
# у 20–60 % треугольников нормаль смотрит против геометрии — поверхности темнеют/бликуют пятнами.
# Пересохранение редактором (коммит 9f5e8625) записало уже испорченные данные.
#
# Инструмент берёт исходные байты Godot 3 из версии до пересохранения (git show <rev>:<файл>),
# сам декодирует вершины по раскладке Godot 3 (VisualServer::mesh_surface_make_offsets_from_format),
# проверяет нормали по геометрии, собирает поверхность заново через ArrayMesh.add_surface_from_arrays
# и подставляет новые данные в текущий файл (материал и имя поверхности не трогаются).
#
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -s <абсолютный путь> -- [--check] [--rev=9f5e8625~1] [файлы...]
# --check — только отчёт: доля «перевёрнутых» нормалей в текущем файле и в декодированных данных Godot 3.
# Без файлов — все файлы, где в <rev> есть "array_data". Godot и Rider закрыты (файлы перезаписываются).

const G3_VERTEX := 1
const G3_NORMAL := 2
const G3_TANGENT := 4
const G3_COLOR := 8
const G3_TEX_UV := 16
const G3_TEX_UV2 := 32
const G3_BONES := 64
const G3_WEIGHTS := 128
const G3_INDEX := 256
const G3_COMPRESS_VERTEX := 512
const G3_COMPRESS_NORMAL := 1024
const G3_COMPRESS_TANGENT := 2048
const G3_COMPRESS_COLOR := 4096
const G3_COMPRESS_TEX_UV := 8192
const G3_COMPRESS_TEX_UV2 := 16384
const G3_COMPRESS_BONES := 32768
const G3_COMPRESS_WEIGHTS := 65536
const G3_USE_2D_VERTICES := 262144
const G3_USE_16_BIT_BONES := 524288
const G3_OCTAHEDRAL := 2097152

var rev := "9f5e8625~1"
var check_only := false
var files: Array[String] = []
var stats := {"files": 0, "surfaces": 0, "fixed": 0, "skipped": 0}

func _init() -> void:
	for a in OS.get_cmdline_user_args():
		if a == "--check":
			check_only = true
		elif a.begins_with("--rev="):
			rev = a.substr(6)
		else:
			files.append(a)
	if files.is_empty():
		var out := []
		OS.execute("git", ["grep", "-l", "\"array_data\"", rev, "--", "*.tscn", "*.tres"], out)
		for line in out[0].split("\n", false):
			files.append(line.substr(line.find(":") + 1).strip_edges())
	for f in files:
		_process_file(f)
	print("FIXMESH итого: файлов %d, поверхностей %d, пересобрано %d, пропущено %d" % [stats.files, stats.surfaces, stats.fixed, stats.skipped])
	quit()

# --- файлы -----------------------------------------------------------------------------------

func _process_file(path: String) -> void:
	var out := []
	OS.execute("git", ["show", "%s:%s" % [rev, path]], out)
	var old_text: String = out[0]
	var old_meshes := _parse_old_meshes(old_text)
	if old_meshes.is_empty():
		return
	stats.files += 1
	var cur_text := FileAccess.get_file_as_string(path)
	var cur_meshes := _find_current_meshes(cur_text)
	if cur_meshes.size() != old_meshes.size():
		push_error("FIXMESH %s: мешей в старой версии %d, в текущей %d — пропущен" % [path, old_meshes.size(), cur_meshes.size()])
		stats.skipped += old_meshes.size()
		return
	# замены с конца файла, чтобы не сдвигать позиции
	var replacements := []
	for m in old_meshes.size():
		var om: Dictionary = old_meshes[m]
		var cm: Dictionary = cur_meshes[m]
		var cur_surfaces: Array = cm.surfaces
		if cur_surfaces.size() != om.surfaces.size():
			push_error("FIXMESH %s: меш %s — поверхностей %d/%d, пропущен" % [path, om.id, om.surfaces.size(), cur_surfaces.size()])
			stats.skipped += om.surfaces.size()
			continue
		for s in om.surfaces.size():
			stats.surfaces += 1
			var old_surf: Dictionary = om.surfaces[s]
			var arrays := _decode_g3(old_surf)
			if arrays.is_empty():
				stats.skipped += 1
				continue
			var cur_span: Vector2i = cur_surfaces[s]
			var cur_dict_text := cur_text.substr(cur_span.x, cur_span.y - cur_span.x)
			var cur_vc := _int_key(cur_dict_text, "vertex_count")
			if cur_vc != old_surf.vertex_count:
				push_error("FIXMESH %s: меш %s поверхность %d — вершин %d/%d, пропущена" % [path, om.id, s, old_surf.vertex_count, cur_vc])
				stats.skipped += 1
				continue
			var bad_new := _bad_normals(arrays)
			if check_only:
				var cur_bad := _bad_current(cur_dict_text)
				print("FIXMESH %s [%s/%d] формат %d: сейчас %.0f%%, Godot 3 %.0f%%" % [path, om.id, s, old_surf.format, cur_bad * 100, bad_new * 100])
				continue
			if bad_new > 0.15:
				# так бывает у тонкой/двусторонней геометрии и в исходной модели (ошейник игрока)
				print("FIXMESH %s [%s/%d]: нормали не сходятся с геометрией и в Godot 3 (%.0f%%)" % [path, om.id, s, bad_new * 100])
			replacements.append([cur_span, _build_surface_text(arrays, old_surf, cur_dict_text)])
			stats.fixed += 1
	if check_only or replacements.is_empty():
		return
	replacements.sort_custom(func(a, b): return a[0].x > b[0].x)
	for r in replacements:
		cur_text = cur_text.substr(0, r[0].x) + r[1] + cur_text.substr(r[0].y)
	# файл может быть ненадолго занят (антивирус, индексатор) — несколько попыток
	var f: FileAccess = null
	for attempt in 20:
		f = FileAccess.open(path, FileAccess.WRITE)
		if f:
			break
		OS.delay_msec(250)
	if f == null:
		push_error("FIXMESH %s: не удалось открыть для записи (%s)" % [path, error_string(FileAccess.get_open_error())])
		stats.skipped += replacements.size()
		return
	f.store_string(cur_text)
	f.close()
	print("FIXMESH %s: пересобрано %d поверхностей" % [path, replacements.size()])

# Старая версия: блоки ArrayMesh с "surfaces/N = { ... }"
func _parse_old_meshes(text: String) -> Array:
	var meshes := []
	for block in _blocks(text):
		var header: String = block.header
		var standalone: bool = block.is_resource and text.begins_with("[gd_resource type=\"ArrayMesh\"")
		if not (standalone or header.begins_with("[sub_resource type=\"ArrayMesh\"")):
			continue
		var body: String = block.body
		if not body.contains("\"array_data\""):
			continue
		var surfaces := []
		var re := RegEx.create_from_string("surfaces/(\\d+) = \\{")
		for m in re.search_all(body):
			var start := m.get_end() - 1
			var end := _match_brace(body, start)
			surfaces.append(_parse_old_surface(body.substr(start, end - start)))
		meshes.append({"id": block.id, "surfaces": surfaces})
	return meshes

# Текущая версия: блоки ArrayMesh с "_surfaces = [{...}, {...}]" — позиции словарей в тексте файла
func _find_current_meshes(text: String) -> Array:
	var meshes := []
	for block in _blocks(text):
		var header: String = block.header
		if not header.contains("ArrayMesh") and not (block.is_resource and text.begins_with("[gd_resource type=\"ArrayMesh\"")):
			continue
		var body: String = block.body
		var p := body.find("_surfaces = [")
		if p < 0:
			continue
		var surfaces := []
		var i := p + "_surfaces = [".length()
		var arr_end := _match_brace(body, i - 1)
		while true:
			var s := body.find("{", i)
			if s < 0 or s > arr_end:
				break
			var e := _match_brace(body, s)
			surfaces.append(Vector2i(block.offset + s, block.offset + e))
			i = e
		meshes.append({"id": block.id, "surfaces": surfaces})
	return meshes

func _blocks(text: String) -> Array:
	var result := []
	var re := RegEx.create_from_string("(?m)^\\[(sub_resource|resource|gd_resource|ext_resource|node|connection|editable)[^\\n]*\\]")
	var ms := re.search_all(text)
	for k in ms.size():
		var m: RegExMatch = ms[k]
		var body_start := m.get_end()
		var body_end := text.length() if k == ms.size() - 1 else ms[k + 1].get_start()
		var header := m.get_string()
		var id_re := RegEx.create_from_string("id=\"?([^\" \\]]+)\"?")
		var idm := id_re.search(header)
		result.append({
			"header": header,
			"id": idm.get_string(1) if idm and header.begins_with("[sub_resource") else "resource",
			"is_resource": header == "[resource]",
			"body": text.substr(body_start, body_end - body_start),
			"offset": body_start,
		})
	return result

func _match_brace(text: String, open_pos: int) -> int:
	var open := text[open_pos]
	var close := "}" if open == "{" else "]"
	var depth := 0
	var in_str := false
	var i := open_pos
	while i < text.length():
		var c := text[i]
		if in_str:
			if c == "\\":
				i += 1
			elif c == "\"":
				in_str = false
		elif c == "\"":
			in_str = true
		elif c == "{" or c == "[":
			depth += 1
		elif c == "}" or c == "]":
			depth -= 1
			if depth == 0:
				return i + 1
		i += 1
	return text.length()

func _parse_old_surface(d: String) -> Dictionary:
	return {
		"format": _int_key(d, "format"),
		"primitive": _int_key(d, "primitive"),
		"vertex_count": _int_key(d, "vertex_count"),
		"index_count": _int_key(d, "index_count"),
		"array_data": _bytes_key(d, "array_data"),
		"index_data": _bytes_key(d, "array_index_data"),
	}

func _int_key(d: String, key: String) -> int:
	var re := RegEx.create_from_string("\"%s\": (-?\\d+)" % key)
	var m := re.search(d)
	return m.get_string(1).to_int() if m else 0

func _bytes_key(d: String, key: String) -> PackedByteArray:
	var p := d.find("\"%s\": " % key)
	if p < 0:
		return PackedByteArray()
	var s := d.find("(", p)
	var e := d.find(")", s)
	var inner := d.substr(s + 1, e - s - 1).strip_edges()
	if inner.begins_with("\""):
		return Marshalls.base64_to_raw(inner.trim_prefix("\"").trim_suffix("\""))
	var result := PackedByteArray()
	var parts := inner.split(",", false)
	result.resize(parts.size())
	for i in parts.size():
		result[i] = parts[i].to_int()
	return result

# --- декодирование Godot 3 ----------------------------------------------------------------------

func _decode_g3(surf: Dictionary) -> Array:
	var fmt: int = surf.format
	var vc: int = surf.vertex_count
	var data: PackedByteArray = surf.array_data
	var oct := fmt & G3_OCTAHEDRAL != 0
	# размеры атрибутов по VisualServer::mesh_surface_make_offsets_from_format (Godot 3.6)
	var sizes := {}
	var vsize := (2 if fmt & G3_USE_2D_VERTICES else 3) * (2 if fmt & G3_COMPRESS_VERTEX else 4)
	if vsize == 6:
		vsize = 8
	sizes[G3_VERTEX] = vsize
	if fmt & G3_NORMAL:
		sizes[G3_NORMAL] = (2 if fmt & G3_COMPRESS_NORMAL else 4) if oct else (4 if fmt & G3_COMPRESS_NORMAL else 12)
	if fmt & G3_TANGENT:
		sizes[G3_TANGENT] = (2 if fmt & G3_COMPRESS_TANGENT else 4) if oct else (4 if fmt & G3_COMPRESS_TANGENT else 16)
	if fmt & G3_COLOR:
		sizes[G3_COLOR] = 4 if fmt & G3_COMPRESS_COLOR else 16
	if fmt & G3_TEX_UV:
		sizes[G3_TEX_UV] = 4 if fmt & G3_COMPRESS_TEX_UV else 8
	if fmt & G3_TEX_UV2:
		sizes[G3_TEX_UV2] = 4 if fmt & G3_COMPRESS_TEX_UV2 else 8
	if fmt & G3_BONES:
		sizes[G3_BONES] = 8 if fmt & G3_USE_16_BIT_BONES else 4
	if fmt & G3_WEIGHTS:
		sizes[G3_WEIGHTS] = 8 if fmt & G3_COMPRESS_WEIGHTS else 16
	var order := [G3_VERTEX, G3_NORMAL, G3_TANGENT, G3_COLOR, G3_TEX_UV, G3_TEX_UV2, G3_BONES, G3_WEIGHTS]
	var offsets := {}
	var stride := 0
	for a in order:
		if sizes.has(a):
			offsets[a] = stride
			stride += sizes[a]
	if stride * vc != data.size():
		push_error("FIXMESH формат %d: размер данных %d, ожидалось %d x %d" % [fmt, data.size(), stride, vc])
		return []

	var verts := PackedVector3Array()
	var normals := PackedVector3Array()
	var tangents := PackedFloat32Array()
	var colors := PackedColorArray()
	var uv := PackedVector2Array()
	var uv2 := PackedVector2Array()
	var bones := PackedInt32Array()
	var weights := PackedFloat32Array()
	for v in vc:
		var b := v * stride
		var o: int = b + offsets[G3_VERTEX]
		if fmt & G3_COMPRESS_VERTEX:
			verts.append(Vector3(data.decode_half(o), data.decode_half(o + 2), 0.0 if fmt & G3_USE_2D_VERTICES else data.decode_half(o + 4)))
		else:
			verts.append(Vector3(data.decode_float(o), data.decode_float(o + 4), 0.0 if fmt & G3_USE_2D_VERTICES else data.decode_float(o + 8)))
		if fmt & G3_NORMAL:
			o = b + offsets[G3_NORMAL]
			if oct:
				var e := _oct_read(data, o, fmt & G3_COMPRESS_NORMAL != 0)
				normals.append(_oct_decode(e))
			elif fmt & G3_COMPRESS_NORMAL:
				normals.append(Vector3(_s8(data, o), _s8(data, o + 1), _s8(data, o + 2)).normalized())
			else:
				normals.append(Vector3(data.decode_float(o), data.decode_float(o + 4), data.decode_float(o + 8)))
		if fmt & G3_TANGENT:
			o = b + offsets[G3_TANGENT]
			var t: Vector3
			var w: float
			if oct:
				var e := _oct_read(data, o, fmt & G3_COMPRESS_TANGENT != 0)
				# Godot 3: y = y * 0.5 + 0.5, знак бинормали — половина диапазона y
				w = 1.0 if e.y >= 0.0 else -1.0
				e.y = absf(e.y) * 2.0 - 1.0
				t = _oct_decode(e)
			elif fmt & G3_COMPRESS_TANGENT:
				t = Vector3(_s8(data, o), _s8(data, o + 1), _s8(data, o + 2)).normalized()
				w = 1.0 if _s8(data, o + 3) >= 0.0 else -1.0
			else:
				t = Vector3(data.decode_float(o), data.decode_float(o + 4), data.decode_float(o + 8))
				w = 1.0 if data.decode_float(o + 12) >= 0.0 else -1.0
			tangents.append_array([t.x, t.y, t.z, w])
		if fmt & G3_COLOR:
			o = b + offsets[G3_COLOR]
			if fmt & G3_COMPRESS_COLOR:
				colors.append(Color(data[o] / 255.0, data[o + 1] / 255.0, data[o + 2] / 255.0, data[o + 3] / 255.0))
			else:
				colors.append(Color(data.decode_float(o), data.decode_float(o + 4), data.decode_float(o + 8), data.decode_float(o + 12)))
		if fmt & G3_TEX_UV:
			o = b + offsets[G3_TEX_UV]
			uv.append(Vector2(data.decode_half(o), data.decode_half(o + 2)) if fmt & G3_COMPRESS_TEX_UV else Vector2(data.decode_float(o), data.decode_float(o + 4)))
		if fmt & G3_TEX_UV2:
			o = b + offsets[G3_TEX_UV2]
			uv2.append(Vector2(data.decode_half(o), data.decode_half(o + 2)) if fmt & G3_COMPRESS_TEX_UV2 else Vector2(data.decode_float(o), data.decode_float(o + 4)))
		if fmt & G3_BONES:
			o = b + offsets[G3_BONES]
			for k in 4:
				bones.append(data.decode_u16(o + k * 2) if fmt & G3_USE_16_BIT_BONES else data[o + k])
		if fmt & G3_WEIGHTS:
			o = b + offsets[G3_WEIGHTS]
			for k in 4:
				weights.append(data.decode_u16(o + k * 2) / 65535.0 if fmt & G3_COMPRESS_WEIGHTS else data.decode_float(o + k * 4))

	var arrays := []
	arrays.resize(Mesh.ARRAY_MAX)
	arrays[Mesh.ARRAY_VERTEX] = verts
	if fmt & G3_NORMAL: arrays[Mesh.ARRAY_NORMAL] = normals
	if fmt & G3_TANGENT: arrays[Mesh.ARRAY_TANGENT] = tangents
	if fmt & G3_COLOR: arrays[Mesh.ARRAY_COLOR] = colors
	if fmt & G3_TEX_UV: arrays[Mesh.ARRAY_TEX_UV] = uv
	if fmt & G3_TEX_UV2: arrays[Mesh.ARRAY_TEX_UV2] = uv2
	if fmt & G3_BONES: arrays[Mesh.ARRAY_BONES] = bones
	if fmt & G3_WEIGHTS: arrays[Mesh.ARRAY_WEIGHTS] = weights
	if fmt & G3_INDEX and surf.index_count > 0:
		var idx := PackedInt32Array()
		var id: PackedByteArray = surf.index_data
		var wide: bool = id.size() == surf.index_count * 4
		for i in surf.index_count:
			idx.append(id.decode_u32(i * 4) if wide else id.decode_u16(i * 2))
		arrays[Mesh.ARRAY_INDEX] = idx
	return arrays

func _s8(data: PackedByteArray, o: int) -> float:
	return maxf(data.decode_s8(o) / 127.0, -1.0)

# октаэдрическая пара в диапазоне [-1, 1]
func _oct_read(data: PackedByteArray, o: int, compressed: bool) -> Vector2:
	if compressed:
		return Vector2(_s8(data, o), _s8(data, o + 1))
	return Vector2(maxf(data.decode_s16(o) / 32767.0, -1.0), maxf(data.decode_s16(o + 2) / 32767.0, -1.0))

func _oct_decode(e: Vector2) -> Vector3:
	var v := Vector3(e.x, e.y, 1.0 - absf(e.x) - absf(e.y))
	var t := maxf(-v.z, 0.0)
	v.x += -t if v.x >= 0.0 else t
	v.y += -t if v.y >= 0.0 else t
	return v.normalized()

# --- проверка и сборка ---------------------------------------------------------------------------

func _bad_normals(arrays: Array) -> float:
	var v: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]
	var n = arrays[Mesh.ARRAY_NORMAL]
	if n == null:
		return 0.0
	var idx = arrays[Mesh.ARRAY_INDEX]
	var has_idx: bool = idx != null and idx.size() > 0
	var count: int = idx.size() if has_idx else v.size()
	var bad := 0
	var total := 0
	for t in range(0, count - 2, 3):
		var a: int = idx[t] if has_idx else t
		var b: int = idx[t + 1] if has_idx else t + 1
		var c: int = idx[t + 2] if has_idx else t + 2
		var g := (v[c] - v[a]).cross(v[b] - v[a])
		if g.length() < 1e-12:
			continue
		total += 1
		if g.dot(n[a] + n[b] + n[c]) < 0.0:
			bad += 1
	return float(bad) / maxi(total, 1)

func _bad_current(dict_text: String) -> float:
	var d = str_to_var(_strip_refs(dict_text))
	if not d is Dictionary:
		return -1.0
	var mesh := ArrayMesh.new()
	mesh.set("_surfaces", [d])
	if mesh.get_surface_count() == 0:
		return -1.0
	return _bad_normals(mesh.surface_get_arrays(0))

func _strip_refs(dict_text: String) -> String:
	var re := RegEx.create_from_string("(ExtResource|SubResource)\\(\"[^\"]*\"\\)")
	return re.sub(dict_text, "null", true)

func _build_surface_text(arrays: Array, old_surf: Dictionary, cur_dict_text: String) -> String:
	var mesh := ArrayMesh.new()
	# номера примитивов сдвинуты: в Godot 3 TRIANGLES = 4 (LINE_LOOP = 3), в Godot 4 TRIANGLES = 3
	var primitive: int = {0: 0, 1: 1, 2: 2, 4: 3, 5: 4}.get(old_surf.primitive, 3)
	mesh.add_surface_from_arrays(primitive, arrays)
	var d: Dictionary = mesh.get("_surfaces")[0]
	d.erase("material")
	d.erase("name")
	# байтовые массивы — в base64, как пишет редактор (иначе файлы раздуваются в 2–3 раза)
	var b64 := {}
	for key in d.keys():
		if d[key] is PackedByteArray:
			b64[key] = Marshalls.raw_to_base64(d[key])
			d[key] = PackedByteArray([0])
	var text := var_to_str(d)
	for key in b64:
		text = text.replace("\"%s\": PackedByteArray(0)" % key, "\"%s\": PackedByteArray(\"%s\")" % [key, b64[key]])
	# материал и имя — из текущего словаря
	var extra := ""
	for key in ["material", "name"]:
		var re := RegEx.create_from_string("(?m)^\"%s\": [^\\n]*?,?$" % key)
		var m := re.search(cur_dict_text)
		if m:
			extra += "\n" + m.get_string().trim_suffix(",") + ","
	return text.substr(0, 1) + extra + text.substr(1)
