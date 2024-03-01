using Godot;

public partial class CheckFall : Node
{
    [Export] private NodePath teleportPointPath;
    [Export] private float fallHeight = -100f;
    private Node3D teleportPoint;

    public DoorTeleport tempDoorTeleport;
    public bool inside;

    public override async void _Ready()
    {
        teleportPoint = GetNode<Node3D>(teleportPointPath);
        await ToSignal(GetTree(), "process_frame");
    }

    public override void _Process(double delta)
    {
        var player = Global.Get().player;
        if (player == null) return;
        
        if (!(player.GlobalTransform.Origin.Y <= fallHeight)) return;
        player.GlobalTransform = Global.SetNewOrigin(player.GlobalTransform, teleportPoint.GlobalTransform.Origin);
        if (inside && tempDoorTeleport != null)
        {
            tempDoorTeleport.otherDoor.Open(player, true);
        }
    }
}
