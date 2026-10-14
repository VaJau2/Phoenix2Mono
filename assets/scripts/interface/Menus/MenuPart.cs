using Godot;

public partial class MenuPart : Control
{
    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);
    }
}
