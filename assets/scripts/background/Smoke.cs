using Godot;

public class Smoke : Spatial
{
    [Export] private Vector3 mapCenter;
    [Export] private int mapSize;

    private Spatial parent;
    
    private string cameraPath = "/root/Main/Scene/Created_CinematicCamera";
    private Spatial camera;

    private Global global;

    public override void _Ready()
    {
        parent = GetParent<Spatial>();
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
            player.Translation.x > mapCenter.x + mapSize
            || player.Translation.y > mapCenter.y + mapSize
            || player.Translation.z > mapCenter.z + mapSize
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
            camera ??= GetNode<Spatial>(cameraPath);
            Translation = new Vector3(camera.Translation.x, Translation.y, camera.Translation.z);
        }
        else
        {
            Translation = new Vector3(player.Translation.x, Translation.y, player.Translation.z);
        }
    }
}
