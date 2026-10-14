using Godot;

public partial class CheckFall : Node
{
    [Export] private NodePath defaultDoorPath;
    [Export] private NodePath teleportPointPath;
    [Export] private float fallHeight = -100f;
    
    public DoorTeleport TempDoorTeleport;
    public bool inside;
    
    private Node3D teleportPoint;

    public override async void _Ready()
    {
        if (defaultDoorPath != null)
        {
            TempDoorTeleport = GetNode<DoorTeleport>(defaultDoorPath);
            inside = true;
        }
        
        teleportPoint = GetNode<Node3D>(teleportPointPath);
        await ToSignal(GetTree(), "idle_frame");
    }

    public override void _Process(float delta)
    {
        var player = Global.Get().player;
        if (player == null) return;
        if (player.GlobalTransform.origin.y > fallHeight) return;
        
        player.GlobalTransform = Global.SetNewOrigin(player.GlobalTransform, teleportPoint.GlobalTransform.origin);
        
        if (inside && TempDoorTeleport != null)
        {
            TempDoorTeleport.otherDoor.Open(player);
        }
    }
}
