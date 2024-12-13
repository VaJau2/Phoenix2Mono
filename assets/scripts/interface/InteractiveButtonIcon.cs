using Godot;

public class InteractiveButtonIcon : ButtonIcon
{
    public override void _Ready()
    {
        base._Ready();
        
        var parent = GetParent<Button>();
        parent.Connect("pressed", this, nameof(_on_mouse_exited));
        parent.Connect("mouse_entered", this, nameof(_on_mouse_entered));
        parent.Connect("mouse_exited", this, nameof(_on_mouse_exited));
    }

    public void _on_mouse_entered()
    {
        Modulate = Colors.Black;
    }

    public void _on_mouse_exited()
    {
        Modulate = Colors.White;
    }
}
