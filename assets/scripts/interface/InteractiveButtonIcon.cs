using Godot;

public partial class InteractiveButtonIcon : ButtonIcon
{
    public override void _Ready()
    {
        base._Ready();
        
        var parent = GetParent<Button>();
        parent.Connect("pressed", new Callable(this, nameof(_on_mouse_exited)));
        parent.Connect("mouse_entered", new Callable(this, nameof(_on_mouse_entered)));
        parent.Connect("mouse_exited", new Callable(this, nameof(_on_mouse_exited)));
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
