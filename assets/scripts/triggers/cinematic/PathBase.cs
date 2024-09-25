using Godot;
using Godot.Collections;

public class PathBase : Path, ISavable
{
    [Export] private float speed = 0.1f;
    
    protected PathFollow pathFollow;
    
    [Signal]
    public delegate void Finished();
    
    public override void _Ready()
    {
        SetPhysicsProcess(false);
        pathFollow = GetNode<PathFollow>("PathFollow");
    }
    
    public override void _PhysicsProcess(float delta)
    {
        var newOffset = pathFollow.UnitOffset + speed * delta;
        
        if (newOffset < 1f)
        {
            pathFollow.UnitOffset = newOffset;
        }
        else Disable();
    }

    public virtual void Enable()
    {
        pathFollow.UnitOffset = 0;
        pathFollow.Rotation = Vector3.Zero;
        SetPhysicsProcess(true);
    }

    protected virtual void Disable()
    {
        SetPhysicsProcess(false);
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "unitOffset", pathFollow.UnitOffset },
            { "isProcess", IsPhysicsProcessing() }
        };
    }

    public void LoadData(Dictionary data)
    {
        pathFollow.UnitOffset = (float)data["unitOffset"];
        SetPhysicsProcess((bool)data["isProcess"]);
    }
}
