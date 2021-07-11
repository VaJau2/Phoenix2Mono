using System;
using Godot;
using Godot.Collections;

//перемещает непися к двери в помещение
//затем телепортирует в точку
public class MoveNpcToDoorTrigger: ActivateOtherTrigger
{ 
    [Export] public string NpcPath; 
    [Export] public NodePath doorPath;
    [Export] public NodePath teleportPointPath;
    
    private Spatial teleportPoint;
    private NpcWithWeapons npc;
    private DoorTeleport door;
    
    private int step;

    public override void _Ready()
    {

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

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        if (!LoadObjects())
        {
            base._on_activate_trigger();
            return;
        }
        
        switch (step)
        {
            case 0:
            case 1:
                SendNpcAndWait();
                return;
            case 2:
                OpenDoor();
                break;
        }
        
        base._on_activate_trigger();
    }

    private bool LoadObjects()
    {
        if (npc != null && door != null && teleportPoint != null) return true;
        if (NpcPath == null || doorPath == null || teleportPointPath == null) return false;
        
        npc = GetNode<NpcWithWeapons>(NpcPath);
        door = GetNode<DoorTeleport>(doorPath);
        teleportPoint = GetNode<Spatial>(teleportPointPath);

        return true;
    }

    private async void SendNpcAndWait()
    {
        step = 1;
        npc.SetNewStartPos(door.GlobalTransform.origin);
        await ToSignal(npc, nameof(NpcWithWeapons.IsCame));

        step = 2;
        _on_activate_trigger();
    }

    private void OpenDoor()
    {
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
    }
    
    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["step"] = step;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            _on_activate_trigger();
        }
    }
}