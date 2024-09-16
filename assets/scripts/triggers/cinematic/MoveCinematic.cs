using Godot;

public class MoveCinematic : PathBase
{
    //[Export] private float speedRot = 0.2f;
    [Export] protected float playerHeadTimer;
    [Export] private PathFollow.RotationModeEnum rotationMode;
    [Export] protected Vector3 cameraAngle;

    /*private Vector3 cameraAngleRad => new 
    (
        Mathf.Deg2Rad(cameraAngle.x), 
        Mathf.Deg2Rad(cameraAngle.y), 
        Mathf.Deg2Rad(cameraAngle.z)
    );*/
    
    protected CutsceneManager cutsceneManager;
    
    public override void _Ready()
    {
        base._Ready();
        pathFollow.RotationMode = rotationMode;

        cutsceneManager = GetNode<CutsceneManager>("../../..");
    }

    /*public override void _PhysicsProcess(float delta)
    {
        if (cinematicCamera.Rotation != cameraAngleRad)
        {
            if (cinematicCamera.Rotation.AngleTo(cameraAngleRad) > 0.1f)
            {
                cinematicCamera.Rotation.Rotated(cameraAngleRad.Normalized(), delta * speedRot);
            }
            else
            {
                cinematicCamera.Rotation = cameraAngleRad;
            }
        }
        
        base._PhysicsProcess(delta);
    }*/
    
    public override void Enable()
    {
        cutsceneManager.SetCameraParent(pathFollow);
        cutsceneManager.ShowPlayerHead(playerHeadTimer);
        cutsceneManager.ChangeCameraAngle(cameraAngle);

        var cameraLocalPos = cutsceneManager.GetCamera().GlobalTranslation - GlobalTranslation;
        Curve.AddPoint(cameraLocalPos, null, null, 0);
        cutsceneManager.GetCamera().Translation = Vector3.Zero;
        
        ResetPathFollow();
        
        base.Enable();
    }

    protected override void Disable()
    {
        base.Disable();
        Curve.RemovePoint(0);
        cutsceneManager.SetCameraParent(null);
        ResetPathFollow();
    }

    protected void ResetPathFollow()
    {
        pathFollow.UnitOffset = 0;
        pathFollow.Rotation = Vector3.Zero;
    }
}
