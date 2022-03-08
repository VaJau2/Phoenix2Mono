using Godot;

public class CollarCheckDistanceTrigger: TriggerBase
{
    const int COLOR_RED = 2;
    const int COLOR_ORANGE = 1;
    const int COLOR_GREEN = 0;

    [Export] private NodePath pointPath;
    [Export] private float[] distances = new float[3];
    [Export] private SpatialMaterial[] colors = new SpatialMaterial[3];
    [Export] private string collarPath;
    [Export] private int colorIndex;
   
    int tempColor;
    Player player;
    MeshInstance collar;
    Spatial point;

    public void SetCollarColor(int newColor)
    {
        if (tempColor == newColor) return;

        collar.Mesh.SurfaceSetMaterial(colorIndex, colors[newColor]);
        tempColor = newColor;
    }

    public override void SetActive(bool newActive)
    {
        player = Global.Get().player;
        collar = player.GetNode<MeshInstance>(collarPath);
        base.SetActive(newActive);
    }

    public override void _on_activate_trigger()
    {
        //TODO: запрогать взрыв
        base._on_activate_trigger();
    }

    public override void _Ready()
    {
        base._Ready();
        point = GetNode<Spatial>(pointPath);
    }

    public override void _Process(float delta)
    {
        if (distances == null) return;
        if (!IsActive) return;

        //TODO: запрогать писк в зависимости от расстояния

        float tempDistance = player.GlobalTransform.origin.DistanceTo(point.GlobalTransform.origin);

        if (tempDistance > distances[COLOR_RED])
        {
            _on_activate_trigger();
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
    }
}
