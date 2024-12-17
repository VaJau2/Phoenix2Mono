extends Area

onready var mirror = get_node("../Mirror/Mirror2")
var playerIn = false
var timer_on = 0
var view_changed = true

var thirdView = false
var mirrorOn = false

func _ready():
	mirror.pixels_per_unit = G.reflections
	mirror.mirrorOn()
	yield(get_tree().create_timer(0.1),"timeout")
	mirror.mirrorOff()


func _on_Area_body_entered(body):
	if body.name == "Player" && !mirrorOn:
		if !weakref(mirror).get_ref():
			return
		if G.reflections != 0:
			mirror.mirrorOn()
			mirrorOn = true
		playerIn = true
		thirdView = G.player.thirdView


func _on_mirrortrigger_body_exited(body):
	if !weakref(mirror).get_ref():
		return
	if body.name == "Player" && !mirror.camera_see:
		#проверка на удаленный объект
		mirror.mirrorOff()
		mirrorOn = false
		playerIn = false


func _process(delta):
	if playerIn:
		if !weakref(mirror).get_ref():
			set_process(false)
			return
		
		if G.reflections == 0:
			if mirrorOn:
				mirror.mirrorOff()
				mirrorOn = false
				mirror.pixels_per_unit = 0
		else:
			if !mirrorOn:
				view_changed = false
				timer_on = 0.1
				
				mirrorOn = true
		
		
		if mirror.pixels_per_unit != G.reflections:
			mirror.pixels_per_unit = G.reflections
			view_changed = false
			timer_on = 0.1
		
		
		if thirdView != G.player.thirdView:
			thirdView = G.player.thirdView
			mirror.mirrorOff()
			view_changed = false
			timer_on = 0.1
		
		if timer_on > 0:
			timer_on -= delta
		elif !view_changed:
			if G.reflections != 0:
				mirror.mirrorOn()
			view_changed = true
