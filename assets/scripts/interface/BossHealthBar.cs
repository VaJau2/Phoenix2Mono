using Godot;

public class BossHealthBar : Control
{
    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);
    }
}
