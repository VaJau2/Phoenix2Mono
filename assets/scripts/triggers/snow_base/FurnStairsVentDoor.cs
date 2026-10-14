using Godot;

namespace FurnStairs;

public partial class FurnStairsVentDoor: FurnDoor, IDoorTeleport
{
    [Export] private NodePath teleportPointPath;
    [Export] private NodePath triggerAreaPath;
    
    public Node3D TeleportPos { get; private set; }

    private FurnStairsTriggerArea triggerArea;

    public override void _Ready()
    {
        base._Ready();
        TeleportPos = GetNode<Node3D>(teleportPointPath);
        triggerArea = GetNode<FurnStairsTriggerArea>(triggerAreaPath);
    }
    
    public override void Interact(PlayerCamera interactor)
    {
        if (triggerArea.MayClimb)
        {
            TeleportToDoor(interactor.Player);
            return;
        }

        base.Interact(interactor);
    }

    private async void TeleportToDoor(Player player)
    {
        if (this is { IsOpen: false })
        {
            ClickFurn();
            await ToSignal(this, SignalName.Opened);
        }

        player.TeleportToDoor(this);
        
        ClickFurn();
    }
}
