using Godot;
using Godot.Collections;

public partial class OpenDoorsTrigger : TriggerBase
{
    [Export] private Array<NodePath> doorPaths;
    [Export] private Array<DoorType> doorTypes;
    
    //если проверяется DoorTeleport, то для закрытия достаточно любой не-пустой строки
    [Export] private Array<string> doorKeysToOpen; 

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;

        for (int i = 0; i < doorPaths.Count; i++)
        {
            var doorPath = doorPaths[i];
            var doorType = doorTypes[i];
            var doorKey = doorKeysToOpen[i];

            switch (doorType)
            {
                case DoorType.Usual:
                    var door1 = GetNode<FurnDoor>(doorPath);
                    door1.myKey = doorKey;
                    break;
                case DoorType.Teleport:
                    var door2 = GetNode<DoorTeleport>(doorPath);
                    door2.Closed = doorKey != "";
                    break;
            }
        }
        
        base.OnActivateTrigger();
    }
}

public enum DoorType {
    Usual,
    Teleport
}