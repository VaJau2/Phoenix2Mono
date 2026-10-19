extends SceneTree

# Проверка «коллизия против видимой геометрии» (B-29): из точки игрока (на 2 м выше) пускаются 24 горизонтальных
# луча; для каждого — расстояние до коллизии (луч физики) и до видимой геометрии (треугольники MeshInstance3D).
# Расхождение > 0.5 м помечается «<<<». Ожидаемые расхождения: невидимые границы карты (sky, fence-collision),
# тайлы GridMap (не MeshInstance3D), упрощённые коробки коллизии зданий.
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -s <абсолютный путь к этому файлу> -- <номер уровня>
var tris := []   # [TriangleMesh, Transform3D, путь]
func _init():
	_run.call_deferred()
func visual_hit(from: Vector3, to: Vector3) -> Array:
	var best := 1e9
	var who := ""
	for t in tris:
		var inv: Transform3D = t[1].affine_inverse()
		var lf: Vector3 = inv * from
		var lt: Vector3 = inv * to
		var r: Dictionary = t[0].intersect_segment(lf, lt)
		if not r.is_empty():
			var d: float = from.distance_to(t[1] * r.position)
			if d < best:
				best = d
				who = t[2]
	return [best, who]
func _run():
	var main: Node = load("res://scenes/Main.tscn").instantiate()
	main.name = "Main"
	root.add_child(main)
	for i in 60: await process_frame
	main.call("LoadLevel", int(OS.get_cmdline_user_args()[0]))
	var scene: Node = null
	while scene == null or scene.get_node_or_null("Player") == null:
		await process_frame
		scene = main.get_node_or_null("Scene")
	for i in 30: await physics_frame
	for mi in scene.find_children("*", "MeshInstance3D", true, false):
		if mi.mesh == null or not mi.is_visible_in_tree(): continue
		if mi.get_parent() is Skeleton3D: continue
		var tm = mi.mesh.generate_triangle_mesh()
		if tm: tris.append([tm, mi.global_transform, str(scene.get_path_to(mi))])
	var space = scene.get_node("Player").get_world_3d().direct_space_state
	var origin: Vector3 = scene.get_node("Player").global_position + Vector3.UP * 2
	for a in 24:
		var ang := a * TAU / 24
		var to := origin + Vector3(sin(ang), 0, cos(ang)) * 60
		var q := PhysicsRayQueryParameters3D.create(origin, to)
		q.exclude = [scene.get_node("Player").get_rid()]
		var r: Dictionary = space.intersect_ray(q)
		var pd := origin.distance_to(r.position) if not r.is_empty() else 1e9
		var pw := str(scene.get_path_to(r.collider)) if not r.is_empty() else "-"
		var v := visual_hit(origin, to)
		printerr("VIS ang=%3d phys=%6.2f (%s) visual=%6.2f (%s)%s" % [a * 15, pd, pw, v[0], v[1], "   <<<" if absf(pd - v[0]) > 0.5 else ""])
	quit()
