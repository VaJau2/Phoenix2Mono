public class ReturnCinematic : AbstractMoveCinematic
{
    public override void Enable()
    {
        if (!smoothTransition)
        {
            Disable();
            return;
        }
        
        var playerCameraCache = cutsceneManager.GetPlayerCameraData();
        cameraAngleRad = playerCameraCache.Rot;
        
        var playerLocalPos = playerCameraCache.Pos - GlobalTranslation;
        Curve.AddPoint(playerLocalPos);
        
        base.Enable();
        
        if (Curve.GetPointCount() > 2)
        {
            Curve.RemovePoint(1);
        }
    }

    protected override void Disable()
    {
        base.Disable();
        Curve.RemovePoint(Curve.GetPointCount() - 1);
        cutsceneManager.ReturnPlayerCamera();
    }
}
