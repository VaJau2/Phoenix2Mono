using Godot;

public class ReturnCinematic : MoveCinematic
{
    private SpatialCache playerCameraCache;

    public override void Enable()
    {
        playerCameraCache = cutsceneManager.GetPlayerCameraData();
        var playerLocalPos = playerCameraCache.Pos - GlobalTranslation;
        Curve.AddPoint(playerLocalPos);
        Curve.RemovePoint(0);
        
        OnEnable();

        if (smoothTransition)
        {
            cutsceneManager.HidePlayerHead(playerHeadTimer);
        }
        else Disable();
    }

    protected override void Disable()
    {
        base.Disable();
        Curve.RemovePoint(Curve.GetPointCount() - 1);
        cutsceneManager.ReturnPlayerCamera();
    }
}
