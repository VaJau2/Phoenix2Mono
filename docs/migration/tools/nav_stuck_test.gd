extends SceneTree

# Проба B-53: застревание NPC при ходьбе по навмешу.
#
# Загружает уровень через DEV-меню. Без --chase игрок отключается и уносится далеко — NPC ходят сами
# (патрули). С --chase игрок бессмертен и раз в 6 с переставляется в случайную точку навмеша рядом
# со случайным NPC — враги преследуют и ищут его (Attack/Search), сбиваются в кучу у одной точки.
# Раз в секунду по каждому NPC с NavigationMovingController: идёт ли (Velocity) и сдвинулся ли.
# «Застрял» — горизонтальная скорость > 0.5, а за секунду сдвинулся меньше чем на 0.5 м; для таких —
# во что упёрся (контакты move_and_slide и угол между скоростью и нормалью) и режим «пули»
# (ComeDistance = 1, RotationSpeed = 0.9 — его включает CheckMovablePath).
# Итог: секунды ходьбы / застревания по NPC, самое долгое застревание подряд, сколько NPC остались
# в режиме «пули» к концу прогона. seed фиксирован — прогоны до и после правки сравнимы.
#
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --fixed-fps 60 --path . \
#     -s <абсолютный путь к этому файлу> -- [--level=7] [--frames=5400] [--chase] > nav.log 2>&1
# До правки B-53 (SnowBase, --chase, 5400 кадров): 140 с застреваний из 598, самое долгое — 20 с подряд,
# 9 NPC навсегда резкие. После: 28 из 508, самое долгое — 4 с, 0.

var level := 7
var frames := 5400
var chase := false

func _init() -> void:
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--level="):
			level = a.substr(8).to_int()
		elif a.begins_with("--frames="):
			frames = a.substr(9).to_int()
		elif a == "--chase":
			chase = true
	_run.call_deferred()

func _run() -> void:
	var main: Node = load("res://scenes/Main.tscn").instantiate()
	main.name = "Main"
	root.add_child(main)
	for i in 60:
		await process_frame
	var menu := main.get_node("Menu/MainMenu/Testing")
	(menu.get_node("levelsList") as OptionButton).select(level - 1)
	(menu.get_node("raceList") as OptionButton).select(0)
	var old_children := main.get_children()
	menu.call("_on_load_pressed")
	var scene: Node = null
	var deadline := Time.get_ticks_msec() + 180000
	while scene == null and Time.get_ticks_msec() < deadline:
		await process_frame
		for c in main.get_children():
			if not c in old_children and c.scene_file_path.begins_with("res://scenes/"):
				scene = c
	if scene == null:
		print("NAV FAIL: level not loaded")
		quit()
		return
	print("NAV level ", scene.scene_file_path)
	for i in 30:
		await process_frame

	var player := scene.get_node_or_null("Player")
	if player and not chase:
		player.process_mode = Node.PROCESS_MODE_DISABLED
		player.global_position += Vector3(0, 0, 3000)

	var npcs := []
	for n in scene.get_tree().get_nodes_in_group("savable"):
		if n is CharacterBody3D and n.has_node("movingController") \
				and n.get_node("movingController").get("ComeDistance") != null:
			npcs.append(n)
	print("NAV npcs: ", npcs.size())
	seed(12345)

	var map = scene.get_viewport().world_3d.navigation_map
	var prev := {}
	var moving := {}
	var stuck := {}
	var streak := {}
	var longest := {}
	var sharp := {}
	for f in frames:
		await process_frame
		if chase and player and is_instance_valid(player):
			player.set("Health", 1000000)
			if f % 360 == 0:
				var alive := npcs.filter(func(x): return is_instance_valid(x))
				var near = alive[randi() % alive.size()]
				var p := NavigationServer3D.map_get_closest_point(map,
					near.global_position + Vector3(randf_range(-25, 25), 0, randf_range(-25, 25)))
				player.global_position = p + Vector3(0, 1, 0)
		if f % 60 != 0:
			continue
		if paused:
			print("NAV PAUSED t=", f / 60)   # меню смерти и т. п. — эти секунды не считаются
			continue
		for n in npcs:
			if not is_instance_valid(n):
				continue
			var mc = n.get_node("movingController")
			var v: Vector3 = n.get("Velocity")
			var hv := Vector2(v.x, v.z).length()
			var pos: Vector3 = n.global_position
			var d := -1.0
			if prev.has(n):
				d = Vector2(pos.x - prev[n].x, pos.z - prev[n].z).length()
			prev[n] = pos
			sharp[n.name] = is_equal_approx(mc.get("RotationSpeed"), 0.9)
			if not (hv > 0.5 and d >= 0):
				continue
			moving[n.name] = moving.get(n.name, 0) + 1
			if d >= 0.5:
				streak[n.name] = 0
				continue
			stuck[n.name] = stuck.get(n.name, 0) + 1
			streak[n.name] = streak.get(n.name, 0) + 1
			longest[n.name] = max(longest.get(n.name, 0), streak[n.name])
			var cols := []
			for i in n.get_slide_collision_count():
				var c: KinematicCollision3D = n.get_slide_collision(i)
				var nrm := c.get_normal()
				var hn := Vector3(nrm.x, 0, nrm.z).normalized()
				var ang := rad_to_deg(acos(clamp(-hn.dot(Vector3(v.x, 0, v.z).normalized()), -1, 1)))
				var col = c.get_collider()
				cols.append("%s angle=%.0f" % [col.name if col else "?", ang])
			print("NAV STUCK %s t=%d state=%s hv=%.1f moved=%.2f come=%.1f rot=%.2f pos=%s cols=%s" % [
				n.name, f / 60, n.call("GetState"), hv, d, mc.get("ComeDistance"), mc.get("RotationSpeed"), pos, cols])

	var total_moving := 0
	var total_stuck := 0
	var max_streak := 0
	for k in moving:
		total_moving += moving[k]
		total_stuck += stuck.get(k, 0)
		max_streak = max(max_streak, longest.get(k, 0))
		print("NAV SUM %s moving=%d stuck=%d longest=%d" % [k, moving[k], stuck.get(k, 0), longest.get(k, 0)])
	print("NAV TOTAL moving=%d stuck=%d longest=%d sharp_at_end=%d" % [
		total_moving, total_stuck, max_streak, sharp.values().count(true)])
	quit()
