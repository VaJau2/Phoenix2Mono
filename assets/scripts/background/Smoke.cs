using Godot;

public partial class Smoke : Node3D
{
    [Export] private Vector3 mapCenter;
    [Export] private int mapSize;

    private Node3D parent;
    
    private string cameraPath = "/root/Main/Scene/Created_CinematicCamera";
    private Node3D camera;

    private Global global;

    public override void _Ready()
    {
        parent = GetParent<Node3D>();
        global = Global.Get();
    }

    public override void _Process(double delta)
    {
        var player = global.player;
        if (player == null) return;
        
        if (Visible != parent.Visible)
        {
            Visible = parent.Visible;
        }

        var isPlayerExiting =
        (
            player.Position.X > mapCenter.X + mapSize
            || player.Position.Y > mapCenter.Y + mapSize
            || player.Position.Z > mapCenter.Z + mapSize
        );
        
        if (camera != null && !isPlayerExiting)
        {
            camera = null;
        }
        
        if (camera != null && !isPlayerExiting)
        {
            camera = null;
        }

        if (Visible && isPlayerExiting)
        {
            camera ??= GetNode<Node3D>(cameraPath);
            Position = new Vector3(camera.Position.X, Position.Y, camera.Position.Z);
        }
        else
        {
            Position = new Vector3(player.Position.X, Position.Y, player.Position.Z);
        }
    }
}
