extends SceneTree

# Проверка масок столкновений (B-49, B-50). В Godot 3 два объекта сталкивались/детектились, если слой одного есть
# в маске ХОТЯ БЫ ОДНОГО из них; в Godot 4 — односторонне: движущееся тело (CharacterBody3D, RigidBody3D) или
# зона (Area3D с monitoring) замечает X, только если слой X есть в ЕЁ маске. Поэтому тело с «пустой» маской,
# которое в Godot 3 держалось за счёт маски пола, в Godot 4 проваливается, а зона перестаёт видеть игрока.
# Скрипт загружает все .tscn в objects/ и scenes/, собирает уникальные сочетания (класс, слой, маска,
# monitoring/monitorable) и печатает пары «SCAN LOST»: A видел X в Godot 3 (A.layer & X.mask), но не видит в
# Godot 4 (X.layer & A.mask == 0). Ожидаемый результат — ни одной такой пары. Изменения слоёв/масок из кода
# (CollisionLayer = ... в C#) не учитываются — их проверять отдельно.
# Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . -s <абсолютный путь к этому файлу> > masks.log 2>&1
var combos := {}  # key -> [class, layer, mask, active, is_area, monitorable, examples(Array)]
func _init() -> void:
	_run.call_deferred()

func _run() -> void:
	var files := []
	_collect("res://objects", files)
	_collect("res://scenes", files)
	for f in files:
		var ps = load(f)
		if not ps is PackedScene: continue
		var st: SceneState = ps.get_state()
		var inst = ps.instantiate(PackedScene.GEN_EDIT_STATE_DISABLED)
		if inst == null: continue
		var nodes := [inst]
		nodes.append_array(inst.find_children("*", "", true, false))
		for n in nodes:
			if not n is CollisionObject3D: continue
			var co: CollisionObject3D = n
			var is_area := co is Area3D
			var active: bool = (is_area and co.monitoring) or co is CharacterBody3D or co is RigidBody3D
			var mon: bool = is_area and co.monitorable
			var key := "%s l=%d m=%d act=%s mon=%s" % [co.get_class(), co.collision_layer, co.collision_mask, active, mon]
			if not combos.has(key):
				combos[key] = [co.get_class(), co.collision_layer, co.collision_mask, active, is_area, mon, []]
			if combos[key][6].size() < 4:
				combos[key][6].append(f.get_file() + ":" + String(inst.get_path_to(co)))
		inst.free()
	print("SCAN combos: ", combos.size())
	for k in combos: print("SCAN combo ", k, "  e.g. ", combos[k][6])
	for ka in combos:
		var a = combos[ka]
		if not a[3]: continue
		for kx in combos:
			var x = combos[kx]
			if x[4] and (not a[4] or not x[5]): continue
			if x[1] == 0: continue
			if (a[1] & x[2]) != 0 and (x[1] & a[2]) == 0:
				print("SCAN LOST  ", ka, "  e.g. ", a[6].slice(0, 2), "   -X-   ", kx, "  e.g. ", x[6].slice(0, 2))
	quit()

func _collect(dir: String, out: Array) -> void:
	var d := DirAccess.open(dir)
	if d == null: return
	for s in d.get_directories(): _collect(dir + "/" + s, out)
	for f in d.get_files():
		if f.ends_with(".tscn"): out.append(dir + "/" + f)
