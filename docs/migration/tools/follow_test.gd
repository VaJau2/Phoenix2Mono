extends SceneTree

# Проба B-52/B-46: следование NPC за игроком (FollowState) и подъём за ним в вентиляцию на SnowBase.
#
# Загружает SnowBase, заставляет заложниц scientist2/scientist3 следовать за игроком (как диалог NpcFollowPlayer),
# ведёт игрока по навмешу к шкафу-лестнице в вентиляцию (кинематически, со скоростью ходьбы),
# затем имитирует подъём (FurnStairsTriggerArea.ClimbToVent: телепорт игрока + сигнал DoorTeleporting)
# и ждёт, пока заложницы поднимутся следом.
#
# Фаза A (B-52): пока игрок идёт, NPC не должны отставать больше чем на MAX_FOLLOW_DISTANCE.
# Фаза B (B-46): NPC должны телепортироваться к teleportPoint шкафа.
# --walk=N — сколько кадров игрок идёт до шкафа (5 м/с); при --walk=1200 и больше — весь путь от мест заложниц.
#
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --fixed-fps 60 --path . \
#     -s <абсолютный путь к этому файлу> -- [--cabinet=main-cabinet] [--walk=600] > follow.log 2>&1
# --fixed-fps 60 — один шаг физики на кадр: игрок (_PhysicsProcess) сдвигается в каждом кадре NPC (_Process),
#     как на машине с FPS = частоте физики. Без него на быстрой машине кадров больше, чем шагов физики.

var cabinet_name := "main-cabinet"
var walk_frames := 600

func _init() -> void:
	for a in OS.get_cmdline_user_args():
		if a.begins_with("--cabinet="):
			cabinet_name = a.substr(10)
		elif a.begins_with("--walk="):
			walk_frames = a.substr(7).to_int()
	_run.call_deferred()

func _run() -> void:
	var main: Node = load("res://scenes/Main.tscn").instantiate()
	main.name = "Main"
	root.add_child(main)
	await _wait(60)

	var menu := main.get_node("Menu/MainMenu/Testing")
	(menu.get_node("levelsList") as OptionButton).select(6)   # SnowBase
	(menu.get_node("raceList") as OptionButton).select(0)
	var old_children := main.get_children()
	menu.call("_on_load_pressed")
	var scene: Node = null
	var deadline := Time.get_ticks_msec() + 180000
	while scene == null and Time.get_ticks_msec() < deadline:
		await process_frame
		for c in main.get_children():
			if not c in old_children and c.scene_file_path.ends_with("SnowBase.tscn"):
				scene = c
	if scene == null:
		_mark("FAIL: SnowBase не загрузился")
		quit()
		return
	await _wait(60)

	var player: CharacterBody3D = scene.get_node("Player")
	var npcs: Array = [scene.get_node("npc/scientist2"), scene.get_node("npc/scientist3")]
	var cabinet: Node3D = scene.get_node("triggers/ventFurnStairs/" + cabinet_name)
	var tp: Node3D = cabinet.get_node("teleportPoint")
	var nav_map: RID = player.get_world_3d().navigation_map

	# путь от заложниц к шкафу; игрок встаёт на него за walk_frames кадров ходьбы до шкафа
	var path := NavigationServer3D.map_get_path(nav_map, npcs[0].global_position, cabinet.global_position, true)
	_mark("путь к %s: %d точек, %.0f м" % [cabinet_name, path.size(), _path_len(path, 0)])
	var start_i := path.size() - 1
	while start_i > 0 and _path_len(path, start_i) < walk_frames / 60.0 * WALK_SPEED:
		start_i -= 1

	# игрок, в отличие от NPC, двери сам не открывает — открываем двери у пути заранее
	for door in scene.find_children("*", "", true, false):
		var scr = door.get_script()
		if scr == null or not scr.resource_path.ends_with("/FurnDoor.cs") or door.get("IsOpen") != false:
			continue
		for i in range(start_i, path.size()):
			if (door as Node3D).global_position.distance_to(path[i]) < 4.0:
				_mark("открываю дверь %s" % scene.get_path_to(door))
				door.call("ClickFurn")
				break
	await _wait(90)

	# start_i < 2 (весь путь): заложницы остаются на своих местах, игрок — рядом с первой
	start_i = max(start_i, 1)
	player.global_position = path[start_i] + Vector3.UP
	for i in npcs.size():
		var npc: Node3D = npcs[i]
		# на точки пути позади игрока: сбоку от пути может не быть навмеша (или он не связан с коридором),
		# тогда весь путь NPC — пара точек ближе ComeDistance, и он стоит независимо от B-52
		if start_i - 1 - i > 0:
			npc.global_position = path[start_i - 1 - i] + Vector3.UP
		npc.get_node("body").set("IdleAnim", "Idle1")
		npc.call("SetFollowTarget", player)
	await _wait(120)

	# --- фаза A: игрок идёт, NPC следуют ---
	var max_dist := [0.0, 0.0]
	var i_point := start_i + 1
	# игрок движется кинематически в шаге физики (как Player._PhysicsProcess): FollowState читает только
	# позицию и Velocity цели, а настоящая ходьба упирается в NPC и мебель
	player.set_physics_process(false)
	for f in walk_frames * 3:
		if i_point >= path.size():
			break
		var to: Vector3 = path[i_point] + Vector3.UP - player.global_position
		if to.length() < 0.3:
			i_point += 1
			continue
		await physics_frame
		var step := to.normalized() * WALK_SPEED / 60.0
		player.global_position += step if step.length() < to.length() else to
		player.velocity = to.normalized() * WALK_SPEED
		player.rotation.y = atan2(-to.x, -to.z)
		await process_frame
		if f > 180:   # первые 3 с NPC догоняют игрока со своих мест
			for n in npcs.size():
				max_dist[n] = max(max_dist[n], npcs[n].global_position.distance_to(player.global_position))
		if f % 60 == 0:
			_mark("A f=%d игрок до шкафа %.1f м | %s" % [f, _path_len(path, i_point) + to.length(), _npc_info(npcs, player)])
			if OS.get_cmdline_user_args().has("--debug"):
				var cam: Camera3D = player.get_viewport().get_camera_3d()
				_mark("   игрок %s точка %d %s v=%s взгляд %s" % [player.global_position, i_point, path[i_point], player.velocity, -cam.global_basis.z if cam else "-"])
	player.velocity = Vector3.ZERO
	player.set_physics_process(true)
	# дистанция следования — ComeDistance * 2 = 6 м, второй NPC идёт в очереди за первым
	for n in npcs.size():
		_mark("A ИТОГ %s: макс. отставание от идущего игрока %.1f м%s" % [
			npcs[n].name, max_dist[n], " FAIL" if max_dist[n] > MAX_FOLLOW_DISTANCE else ""])

	# --- фаза B: игрок поднимается в вентиляцию ---
	await _wait(90)
	_mark("B перед подъёмом | %s" % _npc_info(npcs, player))
	player.global_position = tp.global_position
	player.global_rotation = tp.global_rotation
	player.emit_signal("DoorTeleporting", cabinet)
	var done := [-1, -1]
	for f in 1200:
		await process_frame
		for n in npcs.size():
			if done[n] < 0 and npcs[n].global_position.distance_to(tp.global_position) < 2.0:
				done[n] = f
		if f % 120 == 0:
			_mark("B f=%d | %s" % [f, _npc_info(npcs, cabinet)])
	for n in npcs.size():
		if done[n] >= 0:
			_mark("B ИТОГ %s: поднялся через %d кадров" % [npcs[n].name, done[n]])
		else:
			_mark("B ИТОГ %s: FAIL не поднялся, до шкафа %.1f м, state=%s, скорость %.1f" % [
				npcs[n].name, npcs[n].global_position.distance_to(cabinet.global_position),
				npcs[n].call("GetState"), npcs[n].velocity.length()])
	_mark("ALL DONE")
	quit()

const WALK_SPEED := 5.0   # м/с, больше Character.MIN_WALKING_SPEED (2)
const MAX_FOLLOW_DISTANCE := 25.0   # на углах и у дверей NPC отстают, дальше RUN_DISTANCE (12 м) догоняют бегом

func _npc_info(npcs: Array, target: Node3D) -> String:
	var parts := []
	for npc in npcs:
		parts.append("%s: %.1f м, v=%.1f" % [npc.name, npc.global_position.distance_to(target.global_position),
			Vector2(npc.velocity.x, npc.velocity.z).length()])
	return ", ".join(parts)

func _path_len(path: PackedVector3Array, from: int) -> float:
	var s := 0.0
	for i in range(max(from, 0), path.size() - 1):
		s += path[i].distance_to(path[i + 1])
	return s

func _wait(n: int) -> void:
	for i in n:
		await process_frame

func _mark(text: String) -> void:
	printerr("FOLLOW ", text)
