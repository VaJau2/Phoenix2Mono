using Godot;

public partial class FollowState(
    NavigationMovingController movingController
) : AbstractNpcState
{
    private const float COME_DISTANCE = 10;
    private IDoorTeleport lastDoorTeleport;
    private bool isWaiting;

    public void SetWaiting(bool value)
    {
        isWaiting = value;
    }
    
    public override void Enable(NPC npc)
    {
        base.Enable(npc);

        npc.followTarget.Connect(Character.SignalName.DoorTeleporting, new Callable(this, nameof(OnTargetTeleportToDoor)));
    }

    public override void Disable()
    {
        tempNpc.followTarget?.Disconnect(Character.SignalName.DoorTeleporting, new Callable(this, nameof(OnTargetTeleportToDoor)));
        base.Disable();
    }

    public override void _Process(double delta)
    {
        if (tempNpc.followTarget == null)
        {
            tempNpc.SetState(SetStateEnum.Idle);
            return;
        }

        if (isWaiting)
        {
            movingController.Stop();
            return;
        }

        if (lastDoorTeleport != null)
        {
            GoToLastDoorTeleport();
            return;
        }
        
        FollowTarget();
    }

    private void OnTargetTeleportToDoor(Node3D door)
    {
        if (isWaiting) return;
        if (door is not IDoorTeleport teleport) return;
        lastDoorTeleport = teleport;
    }

    private void GoToLastDoorTeleport()
    {
        movingController.GoTo(lastDoorTeleport.GlobalPosition, COME_DISTANCE);
        if (!movingController.cameToPlace) return;
        
        tempNpc.TeleportToDoor(lastDoorTeleport);
        lastDoorTeleport = null;
    }

    private void FollowTarget()
    {
        var targetPos = tempNpc.followTarget.GlobalPosition;
        movingController.GoTo(targetPos, movingController.ComeDistance * 2f);
        movingController.updatePath = tempNpc.followTarget?.Velocity.Length() > Character.MIN_WALKING_SPEED;
    }
}
