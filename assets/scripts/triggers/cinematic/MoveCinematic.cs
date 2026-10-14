using Godot;

public partial class MoveCinematic : AbstractMoveCinematic
{
    [Export] private PathFollow3D.RotationModeEnum rotationMode;
    [Export] private Vector3 cameraAngle;
    
    public override void _Ready()
    {
        base._Ready();
        pathFollow.RotationMode = rotationMode;
        
        cameraAngleRad = new Vector3(
            Mathf.DegToRad(cameraAngle.X),
            Mathf.DegToRad(cameraAngle.Y),
            Mathf.DegToRad(cameraAngle.Z)
        );
    }

    public override void Disable()
    {
        cutscene.SetCameraParent(null);
        base.Disable();
    }
}
