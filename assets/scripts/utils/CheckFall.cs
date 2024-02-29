using Godot;

public class CheckFall : Node
{
    [Export] private NodePath teleportPointPath;
    [Export] private float fallHeight = -100f;
    private Spatial teleportPoint;

    public DoorTeleport tempDoorTeleport;
    public bool inside;

    public override async void _Ready()
    {
        teleportPoint = GetNode<Spatial>(teleportPointPath);
        await ToSignal(GetTree(), "idle_frame");
    }

    public override void _Process(float delta)
    {
        var player = Global.Get().player;
        if (player == null) return;
        
        if (!(player.GlobalTransform.origin.y <= fallHeight)) return;
        player.GlobalTransform = Global.SetNewOrigin(player.GlobalTransform, teleportPoint.GlobalTransform.origin);
        if (inside && tempDoorTeleport != null)
        {
            tempDoorTeleport.otherDoor.Open(player, true);
        }
    }
}
