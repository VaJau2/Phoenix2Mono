using Godot;

public class MoveCinematic : AbstractMoveCinematic
{
    [Export] private PathFollow.RotationModeEnum rotationMode;
    [Export] private Vector3 cameraAngle;
    
    public override void _Ready()
    {
        base._Ready();
        pathFollow.RotationMode = rotationMode;
        
        cameraAngleRad = new Vector3(
            Mathf.Deg2Rad(cameraAngle.x),
            Mathf.Deg2Rad(cameraAngle.y),
            Mathf.Deg2Rad(cameraAngle.z)
        );
    }

    protected override void Disable()
    {
        base.Disable();
        cutsceneManager.SetCameraParent(null);
        EmitSignal(nameof(Finished));
    }
}
