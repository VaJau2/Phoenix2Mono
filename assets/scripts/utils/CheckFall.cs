using Godot;

public class CheckFall : Node
{
    [Export] private NodePath teleportPointPath;
    [Export] private float fallHeight = -100f;
    private Player player;
    private Spatial teleportPoint;

    public DoorTeleport tempDoorTeleport;
    public bool inside;

    public override async void _Ready()
    {
        teleportPoint = GetNode<Spatial>(teleportPointPath);
        await ToSignal(GetTree(), "idle_frame");
        player = Global.Get().player;
    }

    public override void _Process(float delta)
    {
        if (!(player.GlobalTransform.origin.y <= fallHeight)) return;
        player.GlobalTransform = Global.setNewOrigin(player.GlobalTransform, teleportPoint.GlobalTransform.origin);
        if (inside && tempDoorTeleport != null)
        {
            tempDoorTeleport.otherDoor.Open(player, true);
        }
    }
}
