using System;
using Godot.Collections;
using Godot;

public partial class PathBase : Path3D, ISavable
{
    [Export] private float speed = 0.1f;
    
    protected PathFollow3D pathFollow;
    
    [Signal]
    public delegate void FinishedEventHandler();
    
    public override void _Ready()
    {
        SetPhysicsProcess(false);
        pathFollow = GetNode<PathFollow3D>("PathFollow");
    }
    
    public override void _PhysicsProcess(double delta)
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
        EmitSignal(SignalName.Finished, this);
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
