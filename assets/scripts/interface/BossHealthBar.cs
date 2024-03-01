using Godot;

public partial class BossHealthBar : Control
{
    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);
    }
}
