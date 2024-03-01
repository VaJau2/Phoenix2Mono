using Godot;

public partial class CollarCheckDistanceTrigger: TriggerBase
{
    const int COLOR_RED = 2;
    const int COLOR_ORANGE = 1;
    const int COLOR_GREEN = 0;

    const int EXPLOSION_DAMAGE = 200;

    [Export] private NodePath pointPath;
    [Export] private float[] distances = new float[3];
    [Export] private StandardMaterial3D[] colors = new StandardMaterial3D[3];
    [Export] private float[] peekTimers = new float[3];
    [Export] private AudioStreamWav peekSound;
    [Export] private PackedScene explosionPrefab;
    [Export] private string collarPath;
    [Export] private int colorIndex;

    float peekTimer;
    int tempColor;
    Player player;
    MeshInstance3D collar;
    Node3D point;

    private void UpdatePeeking(float delta)
    {
        if (tempColor == COLOR_GREEN) return;

        if (peekTimer > 0)
        {
            peekTimer -= delta;
        } 
        else
        {
            player.GetAudi(true).Stream = peekSound;
            player.GetAudi(true).Play();
            peekTimer = peekTimers[tempColor];
        }
    }

    public void SetCollarColor(int newColor)
    {
        if (tempColor == newColor) return;

        collar.Mesh.SurfaceSetMaterial(colorIndex, colors[newColor]);
        tempColor = newColor;
    }

    public override void OnActivateTrigger()
    {
        var explosion = explosionPrefab.Instantiate<Explosion>();
        explosion.checkWalls = false;
        collar.AddChild(explosion);
        explosion.Position = Vector3.Zero;
        explosion.Explode();

        player.TakeDamage(player, EXPLOSION_DAMAGE);
        base.OnActivateTrigger();
    }

    public override async void _Ready()
    {
        SetProcess(false);
        await ToSignal(GetTree(), "idle_frame");

        point = GetNode<Node3D>(pointPath);
        player = Global.Get().player;
        collar = player.GetNode<MeshInstance3D>(collarPath);
        peekTimer = peekTimers[COLOR_GREEN];

        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (distances == null) return;
        if (!IsActive) return;

        float tempDistance = player.GlobalTransform.Origin.DistanceTo(point.GlobalTransform.Origin);

        if (tempDistance > distances[COLOR_RED])
        {
            OnActivateTrigger();
        } 
        else if(tempDistance > distances[COLOR_ORANGE])
        {
            SetCollarColor(COLOR_RED);
        }
        else if(tempDistance > distances[COLOR_GREEN])
        {
            SetCollarColor(COLOR_ORANGE);
        }
        else
        {
            SetCollarColor(COLOR_GREEN);
        }

        UpdatePeeking((float)delta);
    }
}
