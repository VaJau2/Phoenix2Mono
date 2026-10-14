extends SceneTree

# Godot 3: пересохраняет бинарные ресурсы (.material, .res, .mesh, .meshlib, .anim, .phymat) в текстовый .tres.
# Встроенные в материалы текстуры (ImageTexture без своего файла) выносятся в .png рядом с материалом,
# одинаковые картинки сохраняются один раз. Для каждого .png пишется .import с флагами исходной текстуры.
#
# Запуск:
#   Godot_v3.6.3-stable_mono_win64.exe --no-window --path . -s docs/migration/tools/convert_binary_resources.gd \
#       --list=<файл со списком res:// путей> --map=<куда записать соответствия "старый<TAB>новый">
#
# Скрипт не удаляет исходные файлы и не правит ссылки на них — это делает convert_binary_resources.sh.

var planned_paths = {}   # пути, которые уже заняты новыми файлами
var image_paths = {}     # md5 картинки -> путь .png
var png_count = 0

func _init():
	var list_path = ""
	var map_path = ""
	for a in OS.get_cmdline_args():
		if a.begins_with("--list="):
			list_path = a.substr(7)
		elif a.begins_with("--map="):
			map_path = a.substr(6)

	var sources = _read_lines(list_path)
	var map_file = File.new()
	map_file.open(map_path, File.WRITE)

	var failed = 0
	for src in sources:
		var dst = _free_path(src.get_basename(), src.get_extension(), "tres")
		var res = ResourceLoader.load(src, "", true)
		if res == null:
			print("FAIL load ", src)
			failed += 1
			continue

		var textures = []
		_collect_embedded_textures(res, null, "", textures, {})
		for item in textures:
			_extract_texture(item[0], item[1], item[2], src)

		var err = ResourceSaver.save(dst, res)
		if err != OK:
			print("FAIL save ", src, " err=", err)
			failed += 1
			continue

		_fix_texture_types(dst)
		map_file.store_line(src + "\t" + dst)
		print("OK ", src, " -> ", dst, " (", res.get_class(), ", png: ", textures.size(), ")")

	map_file.close()
	print("DONE total=", sources.size(), " failed=", failed, " png=", png_count)
	quit()

# Ищем ImageTexture без собственного файла, которые лежат внутри материалов.
# Превью тайлов MeshLibrary и прочее не трогаем.
func _collect_embedded_textures(value, owner, slot, result, visited):
	if value is Resource:
		if visited.has(value):
			return
		visited[value] = true
		if value is ImageTexture and owner is Material and _is_embedded(value):
			result.append([value, owner, slot])
			return
		for p in value.get_property_list():
			if p.usage & PROPERTY_USAGE_STORAGE:
				_collect_embedded_textures(value.get(p.name), value, p.name, result, visited)
	elif value is Array:
		for v in value:
			_collect_embedded_textures(v, owner, slot, result, visited)
	elif value is Dictionary:
		for k in value:
			_collect_embedded_textures(value[k], owner, slot, result, visited)

func _is_embedded(res):
	return res.resource_path == "" or res.resource_path.find("::") != -1

func _extract_texture(tex, material, slot, src):
	var img = tex.get_data()
	if img == null:
		print("WARN empty texture in ", src)
		return
	if img.is_compressed():
		img.decompress()

	var ctx = HashingContext.new()
	ctx.start(HashingContext.HASH_MD5)
	ctx.update(img.get_data())
	var hash_key = ctx.finish().hex_encode() + "_" + str(img.get_size()) + "_" + str(tex.flags)

	var png_path = image_paths.get(hash_key, "")
	if png_path == "":
		var name = tex.resource_name
		if name == "" or name.find("/") != -1:
			name = material.resource_name if material.resource_name != "" else src.get_file().get_basename()
		name = name.replace(":", "_").replace("/", "_").replace("\\", "_")
		png_path = _free_path(src.get_base_dir().plus_file(name), "", "png")
		if img.save_png(png_path) != OK:
			print("WARN cannot save ", png_path)
			return
		# цвет — в sRGB, данные (нормали, metallic, roughness, ao) — линейные
		var srgb = slot == "albedo_texture" or slot == "emission_texture"
		_write_import(png_path, tex.flags, srgb)
		image_paths[hash_key] = png_path
		png_count += 1

	tex.take_over_path(png_path)

# .import с настройками, как у остальных 3D-текстур проекта: без фильтрации и mipmap-карт (пиксельный стиль),
# без VRAM-сжатия. detect_3d выключен — иначе Godot сам включит фильтрацию при первом использовании в 3D.
# Флаги исходной ImageTexture (обычно filter + mipmaps + repeat от импорта .glb) сознательно не переносятся,
# кроме repeat. Godot 3 импортирует .png при следующем открытии редактора.
func _write_import(png_path, flags, srgb):
	var f = File.new()
	f.open(png_path + ".import", File.WRITE)
	f.store_string("[remap]\n\nimporter=\"texture\"\ntype=\"CompressedTexture2D\"\n\n")
	f.store_string("[deps]\n\nsource_file=\"" + png_path + "\"\n\n")
	f.store_string("[params]\n\n")
	f.store_string("compress/mode=0\ncompress/lossy_quality=0.7\ncompress/hdr_mode=0\ncompress/bptc_ldr=0\n")
	f.store_string("compress/normal_map=0\nflags/repeat=" + str(1 if flags & Texture2D.FLAG_REPEAT else 0) + "\n")
	f.store_string("flags/filter=false\nflags/mipmaps=false\nflags/anisotropic=false\n")
	f.store_string("flags/srgb=" + str(1 if srgb else 0) + "\nprocess/fix_alpha_border=true\nprocess/premult_alpha=false\n")
	f.store_string("process/HDR_as_SRGB=false\nprocess/invert_color=false\nprocess/normal_map_invert_y=false\n")
	f.store_string("stream=false\nsize_limit=0\ndetect_3d=false\nsvg/scale=1.0\n")
	f.close()

# Сохранённый .png импортируется как StreamTexture, поэтому ссылку надо объявить типом Texture,
# а не ImageTexture — иначе загрузчик не примет файл.
func _fix_texture_types(path):
	var f = File.new()
	f.open(path, File.READ)
	var text = f.get_as_text()
	f.close()
	var regex = RegEx.new()
	regex.compile("(\\[ext_resource path=\"[^\"]+\\.png\") type=\"ImageTexture\"")
	var fixed = regex.sub(text, "$1 type=\"Texture2D\"", true)
	if fixed != text:
		f.open(path, File.WRITE)
		f.store_string(fixed)
		f.close()

# base.ext, если занято — base_<old_ext>.ext, base_2.ext ...
func _free_path(base, old_ext, ext):
	var candidates = [base + "." + ext]
	if old_ext != "":
		candidates.append(base + "_" + old_ext + "." + ext)
	for i in range(2, 100):
		candidates.append(base + "_" + str(i) + "." + ext)
	var dir = DirAccess.new()
	for c in candidates:
		if not planned_paths.has(c) and not dir.file_exists(c):
			planned_paths[c] = true
			return c
	return ""

func _read_lines(path):
	var result = []
	var f = File.new()
	if f.open(path, File.READ) != OK:
		printerr("cannot open ", path)
		return result
	while not f.eof_reached():
		var line = f.get_line().strip_edges()
		if line != "":
			result.append(line)
	f.close()
	return result
