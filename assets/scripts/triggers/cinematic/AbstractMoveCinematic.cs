using Godot;

public abstract class AbstractMoveCinematic : PathBase
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
    
    public override void _PhysicsProcess(float delta)
    {
        if (speedRot > 0)
        {
            var camera = cutscene.GetCamera();
            if (camera.GlobalRotation != cameraAngleRad)
            {
                var rot = camera.GlobalRotation;
                rot.x = Mathf.LerpAngle(rot.x, cameraAngleRad.x, delta * speedRot);
                rot.y = Mathf.LerpAngle(rot.y, cameraAngleRad.y, delta * speedRot);
                rot.z = Mathf.LerpAngle(rot.z, cameraAngleRad.z, delta * speedRot);
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
            var cameraLocalPos = camera.GlobalTranslation - GlobalTranslation;
            Curve.AddPoint(cameraLocalPos, null, null, 0);
        }
        
        camera.Translation = Vector3.Zero;
        base.Enable();
    }
    
    public override void Disable()
    {
        SetPhysicsProcess(false);
        if (smoothTransition) Curve.RemovePoint(0);
        EmitSignal(nameof(Finished), this);
    }
}
