using Godot;

//перемещает непися к двери в помещение
//затем телепортирует в точку
public class MoveNpcToDoor: TriggerBase
{ 
    [Export] public NodePath NpcPath; 
    [Export] public NodePath doorPath;
    [Export] public NodePath teleportPointPath;
    
    private Spatial teleportPoint;
    private NpcWithWeapons npc;
    private DoorTeleport door;

    public override void _Ready()
    {
        if (NpcPath != null)
        {
            npc = GetNode<NpcWithWeapons>(NpcPath);
        }

        if (doorPath != null)
        {
            door = GetNode<DoorTeleport>(doorPath);
        }
        
        if (teleportPointPath != null)
        {
            teleportPoint = GetNode<Spatial>(teleportPointPath);
        }

        if (IsActive)
        {
            _on_activate_trigger();
        }
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override async void _on_activate_trigger()
    {
        if (IsActive)
        {
            npc.SetNewStartPos(door.GlobalTransform.origin);
            await ToSignal(npc, nameof(NpcWithWeapons.IsCame));
            
            door.SoundOpening();

            var newPos = teleportPoint.GlobalTransform.origin;
            var newRot = teleportPoint.Rotation;
            npc.SetNewStartPos(newPos);
            npc.myStartRot = newRot;
            
            npc.GlobalTransform = Global.setNewOrigin(npc.GlobalTransform, newPos);
            npc.Rotation = new Vector3(
                npc.Rotation.x,
                newRot.y,
                npc.Rotation.z
            );
            
            
            base._on_activate_trigger();
        }
    }
}