using Godot;

public abstract partial class AbstractMoveCinematic : PathBase
{
    [Export] private float speedRot = 1f;
    [Export] protected bool smoothTransition = true;
    
    protected Vector3 cameraAngleRad;
    protected Cutscene cutscene;
    
    public override void _Ready()
    {
        base._Ready();
        cutscene = GetNode<Cutscene>("../../");
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (speedRot > 0)
        {
            var camera = cutscene.GetCamera();
            if (camera.GlobalRotation != cameraAngleRad)
            {
                var rot = camera.GlobalRotation;
                rot.X = Mathf.LerpAngle(rot.X, cameraAngleRad.X, delta * speedRot);
                rot.Y = Mathf.LerpAngle(rot.Y, cameraAngleRad.Y, delta * speedRot);
                rot.Z = Mathf.LerpAngle(rot.Z, cameraAngleRad.Z, delta * speedRot);
                camera.GlobalRotation = rot;
            }
        }
        
        base._PhysicsProcess(delta);
    }
    
    public override void Enable()
    {
        cutscene.SetCameraParent(pathFollow);
        var camera = cutscene.GetCamera();

        if (smoothTransition)
        {
            var cameraLocalPos = camera.GlobalPosition - GlobalPosition;
            Curve.AddPoint(cameraLocalPos, null, null, 0);
        }
        
        camera.Position = Vector3.Zero;
        base.Enable();
    }
    
    public override void Disable()
    {
        SetPhysicsProcess(false);
        if (smoothTransition) Curve.RemovePoint(0);
        EmitSignal(SignalName.Finished, this);
    }
}
