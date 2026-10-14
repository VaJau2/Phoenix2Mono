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

    public override void _Process(float delta)
    {
        var player = global.player;
        if (player == null) return;
        
        if (Visible != parent.Visible)
        {
            Visible = parent.Visible;
        }

        var isPlayerExiting =
        (
            player.Position.x > mapCenter.x + mapSize
            || player.Position.y > mapCenter.y + mapSize
            || player.Position.z > mapCenter.z + mapSize
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
            Position = new Vector3(camera.Position.x, Position.y, camera.Position.z);
        }
        else
        {
            Position = new Vector3(player.Position.x, Position.y, player.Position.z);
        }
    }
}
