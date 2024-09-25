using Godot;

public abstract class AbstractMoveCinematic : PathBase
{
    [Export] private float speedRot = 1f;
    [Export] protected bool smoothTransition = true;
    
    protected Vector3 cameraAngleRad;
    protected CutsceneManager cutsceneManager;
    
    public override void _Ready()
    {
        base._Ready();
        cutsceneManager = GetNode<CutsceneManager>("../../..");
    }
    
    public override void _PhysicsProcess(float delta)
    {
        var camera = cutsceneManager.GetCamera();
        
        if (camera.GlobalRotation != cameraAngleRad)
        {
            camera.GlobalRotation = camera.GlobalRotation.AngleTo(cameraAngleRad) > 0.0001f 
                ? camera.GlobalRotation.LinearInterpolate(cameraAngleRad, delta * speedRot) 
                : cameraAngleRad;
        }
        
        base._PhysicsProcess(delta);
    }
    
    public override void Enable()
    {
        cutsceneManager.SetCameraParent(pathFollow);
        var camera = cutsceneManager.GetCamera();

        if (smoothTransition)
        {
            var cameraLocalPos = camera.GlobalTranslation - GlobalTranslation;
            Curve.AddPoint(cameraLocalPos, null, null, 0);
        }
        
        camera.Translation = Vector3.Zero;
        base.Enable();
    }
    
    protected override void Disable()
    {
        base.Disable();
        if (smoothTransition) Curve.RemovePoint(0);
    }
}
