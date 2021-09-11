using Godot;

public class CheckFall : Area
{
    [Export] private NodePath teleportPointPath;
    private Spatial teleportPoint;

    public DoorTeleport tempDoorTeleport;
    public bool inside;

    public override void _Ready()
    {
        teleportPoint = GetNode<Spatial>(teleportPointPath);
    }

    public void _on_body_entered(Node body)
    {
        if (body is Player player)
        {
            player.GlobalTransform = Global.setNewOrigin(player.GlobalTransform, teleportPoint.GlobalTransform.origin);
            if (inside && tempDoorTeleport != null)
            {
                tempDoorTeleport.otherDoor.Open(player, true);
            }
        }
    }
}
