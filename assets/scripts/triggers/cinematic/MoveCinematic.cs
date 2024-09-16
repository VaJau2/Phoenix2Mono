using Godot;

public class MoveCinematic : PathBase
{
    [Export] private float speedRot = 0.2f;
    [Export] private PathFollow.RotationModeEnum rotationMode;
    [Export] private Vector3 cameraAngle;

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
        GD.Print($"current camera pos: {cutsceneManager.GetCamera().GlobalTranslation}");
    }*/
    
    public override void Enable()
    {
        cutsceneManager.SetCameraParent(pathFollow);
        cutsceneManager.ChangeCameraAngle(cameraAngle);
        var startLocalPos = cutsceneManager.GetCamera().GlobalTranslation - GlobalTranslation;
        Curve.AddPoint(startLocalPos, null, null, 0);
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

    private void ResetPathFollow()
    {
        pathFollow.UnitOffset = 0;
        pathFollow.Rotation = Vector3.Zero;
    }
}
