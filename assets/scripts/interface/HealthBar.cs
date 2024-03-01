using Godot;

public partial class HealthBar : Control
{
    private const int SIZE_X = 65;
    private const int SIZE_Y = 80;
    
    private Player player;
    private Control mask;
    private Control radiationMask;

    public override void _Ready()
    {
        mask = GetNode<Control>("mask");
        radiationMask = GetNode<Control>("radiationMask");
        MenuBase.LoadColorForChildren(this);
    }

    public override void _Process(double delta)
    {
        player = Global.Get().player;
        if (player == null) return;
        
        var healthRatio = (float)player.Health / player.HealthMax;
        mask.Size = new Vector2(SIZE_X, healthRatio * SIZE_Y);

        var radiationRatio = (float)player.Radiation.GetRadLevel() / player.HealthMax;
        radiationMask.Size = new Vector2(SIZE_X, radiationRatio * SIZE_Y);
    }
}
