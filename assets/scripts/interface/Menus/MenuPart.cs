using Godot;

public class MenuPart : Control
{
    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);
    }
}
