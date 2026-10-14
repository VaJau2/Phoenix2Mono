using Godot;

public class HealthBar : Control
{
    private const int SIZE_X = 65;
    private const int SIZE_Y = 80;
    
    private Player player;
    private Control mask;
    private Control radiationMask;

    private PackedScene healthPrefab;
    private PackedScene radiationPrefab;
    
    public override void _Ready()
    {
        mask = GetNode<Control>("mask");
        healthPrefab = GD.Load<PackedScene>("res://objects/interface/healthBar.tscn");
        
        radiationMask = GetNode<Control>("radiationMask");
        radiationPrefab = GD.Load<PackedScene>("res://objects/interface/radiationBar.tscn");
        
        MenuBase.LoadColorForChildren(this);
    }

    public override void _Process(float delta)
    {
        player = Global.Get().player;
        if (player == null) return;

        UpdateHealthBar();
        UpdateRadiationBar();
    }

    private void UpdateHealthBar()
    {
        if (float.IsNaN(mask.RectSize.y))
        {
            mask.QueueFree();
            mask = healthPrefab.Instance<Control>();
            mask.Modulate = Global.Get().Settings.interfaceColor;
            AddChild(mask);
        }
        
        var healthRatio = (float)player.Health / player.HealthMax;
        mask.RectSize = new Vector2(SIZE_X, healthRatio * SIZE_Y);
    }
    
    private void UpdateRadiationBar()
    {
        if (float.IsNaN(radiationMask.RectSize.y))
        {
            radiationMask.QueueFree();
            radiationMask = radiationPrefab.Instance<Control>();
            AddChild(radiationMask);
        }
        
        var radiationRatio = (float)player.Radiation.GetRadLevel() / player.HealthMax;
        radiationMask.RectSize = new Vector2(SIZE_X, radiationRatio * SIZE_Y);
    }
}
