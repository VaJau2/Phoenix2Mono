using Godot;

public class PonyStartIdleAnim : Node
{
    [Export] private string Anim;
    
    public override void _Ready()
    {
        var body = GetNodeOrNull<PonyBody>("../body");
        if (body != null)
        {
            body.IdleAnim = Anim;
        }
    }
}
