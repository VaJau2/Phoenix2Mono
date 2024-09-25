using Godot;

public class ReturnCinematic : AbstractMoveCinematic
{
    private SpatialCache playerCameraCache;

    public override void Enable()
    {
        playerCameraCache = cutsceneManager.GetPlayerCameraData();
        cameraAngleRad = playerCameraCache.Rot;
        
        var playerLocalPos = playerCameraCache.Pos - GlobalTranslation;
        Curve.AddPoint(playerLocalPos);
        Curve.RemovePoint(0);
        
        base.Enable();

        if (!smoothTransition) Disable();
    }

    protected override void Disable()
    {
        base.Disable();
        Curve.RemovePoint(Curve.GetPointCount() - 1);
        cutsceneManager.ReturnPlayerCamera();
    }
}
