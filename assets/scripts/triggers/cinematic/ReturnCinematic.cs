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
        
        cutsceneManager.SetCameraParent(pathFollow);
        cutsceneManager.HidePlayerHead(playerHeadTimer);
        cutsceneManager.ChangeCameraAngle(cameraAngle);

        var cameraLocalPos = cutsceneManager.GetCamera().GlobalTranslation - GlobalTranslation;
        Curve.AddPoint(cameraLocalPos, null, null, 0);
        cutsceneManager.GetCamera().Translation = Vector3.Zero;
        
        ResetPathFollow();
        
        SetPhysicsProcess(true);
    }

    protected override void Disable()
    {
        Curve.RemovePoint(Curve.GetPointCount() - 1);
        cutsceneManager.ReturnPlayerCamera();
        base.Disable();
    }
}
