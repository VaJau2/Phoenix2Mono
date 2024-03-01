using Godot;

public partial class DragonFallArea : Area3D
{
    public void _on_fallarea_body_entered(Node body)
    {
        var dragon = GetParent<Dragon>();
        if (dragon.Health <= 0 && dragon.isFalling)
        {
            dragon.isFalling = false;
        }
    }
}
