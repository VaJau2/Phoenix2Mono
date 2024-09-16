using Godot;

public class ReturnCinematic : MoveCinematic
{
    private SpatialCache cameraCache;
    
    public override void _Ready()
    {
        base._Ready();
        cameraCache = cutsceneManager.GetPlayerCameraData();
    }

    public override void Enable()
    {
        var playerLocalPos = cameraCache.Pos - GlobalTranslation;
        Curve.AddPoint(playerLocalPos);
        base.Enable();
    }

    protected override void Disable()
    {
        Curve.RemovePoint(Curve.GetPointCount() - 1);
        cutsceneManager.ReturnPlayerCamera();
        base.Disable();
    }
}
