using Godot;

public class FollowState(
    NavigationMovingController movingController
) : AbstractNpcState
{
    private const float COME_DISTANCE = 10;
    private IDoorTeleport lastDoorTeleport;
    
    public override void Enable(NPC npc)
    {
        base.Enable(npc);

        npc.followTarget.Connect(nameof(Character.DoorTeleporting), this, nameof(OnTargetTeleportToDoor));
    }

    public override void Disable()
    {
        tempNpc.followTarget?.Disconnect(nameof(Character.DoorTeleporting), this, nameof(OnTargetTeleportToDoor));
        base.Disable();
    }

    public override void _Process(float delta)
    {
        if (tempNpc.followTarget == null)
        {
            tempNpc.SetState(SetStateEnum.Idle);
            return;
        }

        if (lastDoorTeleport != null)
        {
            GoToLastDoorTeleport();
            return;
        }
        
        FollowTarget();
    }

    private void OnTargetTeleportToDoor(Spatial door)
    {
        if (door is not IDoorTeleport teleport) return;
        lastDoorTeleport = teleport;
    }

    private void GoToLastDoorTeleport()
    {
        movingController.GoTo(lastDoorTeleport.GlobalTranslation, COME_DISTANCE);
        if (!movingController.cameToPlace) return;
        
        tempNpc.TeleportToDoor(lastDoorTeleport);
        lastDoorTeleport = null;
    }

    private void FollowTarget()
    {
        var targetPos = tempNpc.followTarget.GlobalTranslation;
        movingController.GoTo(targetPos, movingController.ComeDistance * 2f);
        movingController.updatePath = tempNpc.followTarget?.Velocity.Length() > Character.MIN_WALKING_SPEED;
    }
}
