using Godot;

public partial class HealthBar : Control
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

    public override void _Process(double delta)
    {
        player = Global.Get().player;
        if (player == null) return;

        UpdateHealthBar();
        UpdateRadiationBar();
    }

    private void UpdateHealthBar()
    {
        if (float.IsNaN(mask.Size.Y))
        {
            mask.QueueFree();
            mask = healthPrefab.Instantiate<Control>();
            mask.Modulate = Global.Get().Settings.interfaceColor;
            AddChild(mask);
        }
        
        var healthRatio = (float)player.Health / player.HealthMax;
        mask.Size = new Vector2(SIZE_X, healthRatio * SIZE_Y);
    }
    
    private void UpdateRadiationBar()
    {
        if (float.IsNaN(radiationMask.Size.Y))
        {
            radiationMask.QueueFree();
            radiationMask = radiationPrefab.Instantiate<Control>();
            AddChild(radiationMask);
        }
        
        var radiationRatio = (float)player.Radiation.GetRadLevel() / player.HealthMax;
        radiationMask.Size = new Vector2(SIZE_X, radiationRatio * SIZE_Y);
    }
}
