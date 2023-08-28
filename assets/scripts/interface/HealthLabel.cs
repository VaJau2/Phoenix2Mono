using Godot;

public class HealthLabel : TextureRect
{
    Player player;

    public override void _Ready()
    {
        player = Global.Get().player;
        MenuBase.LoadColorForChildren(this);
    }
}
