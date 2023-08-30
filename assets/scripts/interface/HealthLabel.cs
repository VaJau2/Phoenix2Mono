using Godot;

public class HealthLabel : Control
{
    private Player player;
    private Control mask;

    public override void _Ready()
    {
        mask = GetNode<Control>("mask");
        MenuBase.LoadColorForChildren(this);
    }

    public override void _Process(float delta)
    {
        if (player == null)
        {
            player = Global.Get().player;
        }
        else
        {
            float ratio = (float)player.Health / player.HealthMax;
            mask.RectSize = new Vector2(65, ratio * 80);
        }
    }
}
