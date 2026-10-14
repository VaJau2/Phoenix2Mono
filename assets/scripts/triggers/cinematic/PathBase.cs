using System;
using Godot.Collections;
using Godot;

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

    public virtual void Disable()
    {
        SetPhysicsProcess(false);
        EmitSignal(nameof(Finished), this);
    }

    public virtual Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "unitOffset", pathFollow.UnitOffset },
            { "rotation", pathFollow.Rotation },
            { "isProcess", IsPhysicsProcessing() }
        };
    }

    public virtual void LoadData(Dictionary data)
    {
        if (!data.Contains("isProcess")) return;
        if (!Convert.ToBoolean(data["isProcess"])) return;
        
        Enable();
        pathFollow.UnitOffset = (float)data["unitOffset"];
        pathFollow.Rotation = data["rotation"].ToString().ParseToVector3();
    }
}
