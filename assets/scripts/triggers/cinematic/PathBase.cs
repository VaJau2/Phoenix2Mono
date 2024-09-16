using Godot;

public class PathBase : Path
{
    [Export] private float speed = 0.1f;
    
    protected PathFollow pathFollow;
    
    public override void _Ready()
    {
        pathFollow = GetNode<PathFollow>("PathFollow");
        SetPhysicsProcess(false);
    }
    
    public override void _PhysicsProcess(float delta)
    {
        var newOffset = pathFollow.UnitOffset + speed * delta;
        
        if (newOffset < 1f)
        {
            pathFollow.UnitOffset = newOffset;
        }
        else
        {
            Disable();
        }
    }

    public virtual void Enable()
    {
        SetPhysicsProcess(true);
    }

    protected virtual void Disable()
    {
        SetPhysicsProcess(false);
    }
}
