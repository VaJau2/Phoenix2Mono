using System;
using Godot;
using Godot.Collections;

//перемещает непися к двери в помещение
//затем телепортирует в точку
public partial class MoveNpcToDoorTrigger: ActivateOtherTrigger
{ 
    [Export] public string NpcPath; 
    [Export] public NodePath doorPath;
    [Export] public NodePath teleportPointPath;
    
    private Node3D teleportPoint;
    private NpcWithWeapons npc;
    private DoorTeleport door;
    
    private int step;

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        OnActivateTrigger();
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        if (!LoadObjects())
        {
            base.OnActivateTrigger();
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
        
        base.OnActivateTrigger();
    }

    private bool LoadObjects()
    {
        if (npc != null && door != null && teleportPoint != null) return true;
        if (NpcPath == null || doorPath == null || teleportPointPath == null) return false;
        
        npc = GetNode<NpcWithWeapons>(NpcPath);
        door = GetNode<DoorTeleport>(doorPath);
        teleportPoint = GetNode<Node3D>(teleportPointPath);

        return true;
    }

    private async void SendNpcAndWait()
    {
        step = 1;
        npc.SetNewStartPos(door.GlobalTransform.Origin);
        await ToSignal(npc, nameof(NpcWithWeapons.IsCameEventHandler));
        if (npc.state == NPCState.Attack) return;

        step = 2;
        OnActivateTrigger();
    }

    private void OpenDoor()
    {
        door.SoundOpening();

        var newPos = teleportPoint.GlobalTransform.Origin;
        var newRot = teleportPoint.Rotation;
        npc.SetNewStartPos(newPos);
        npc.myStartRot = newRot;
            
        npc.GlobalTransform = Global.SetNewOrigin(npc.GlobalTransform, newPos);
        npc.Rotation = new Vector3(
            npc.Rotation.X,
            newRot.Y,
            npc.Rotation.Z
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
            SetActive(true);
        }
    }
    
    public override void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        
        npc = GetNode<NpcWithWeapons>(NpcPath);
        if (IsInstanceValid(npc) && npc.Health > 0)
        {
            SetActive(true);
            OnActivateTrigger();
        }
    }
}