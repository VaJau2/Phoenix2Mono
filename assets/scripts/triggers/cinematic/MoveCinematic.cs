using Godot;

public class MoveCinematic : PathBase
{
    [Export] private float speedRot = 1f;
    [Export] protected float playerHeadTimer;
    [Export] private PathFollow.RotationModeEnum rotationMode;
    [Export] private Vector3 cameraAngle;
    [Export] protected bool smoothTransition = true;

    protected Vector3 cameraAngleRad;
    
    protected CutsceneManager cutsceneManager;
    
    public override void _Ready()
    {
        base._Ready();
        pathFollow.RotationMode = rotationMode;

        cutsceneManager = GetNode<CutsceneManager>("../../..");
        cameraAngleRad = new Vector3(
            Mathf.Deg2Rad(cameraAngle.x),
            Mathf.Deg2Rad(cameraAngle.y),
            Mathf.Deg2Rad(cameraAngle.z)
        );
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
        OnEnable();
        cutsceneManager.ShowPlayerHead(playerHeadTimer);
    }

    protected void OnEnable()
    {
        cutsceneManager.SetCameraParent(pathFollow);

        if (smoothTransition)
        {
            var cameraLocalPos = cutsceneManager.GetCamera().GlobalTranslation - GlobalTranslation;
            Curve.AddPoint(cameraLocalPos, null, null, 0);
        }
        
        cutsceneManager.GetCamera().Translation = Vector3.Zero;
        base.Enable();
    }

    protected override void Disable()
    {
        base.Disable();
        if (smoothTransition) Curve.RemovePoint(0);
        cutsceneManager.SetCameraParent(null);
        
        EmitSignal(nameof(Finished));
    }
}
