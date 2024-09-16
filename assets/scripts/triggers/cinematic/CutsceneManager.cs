using Godot;

public class CutsceneManager : Node
{
    private Camera cutsceneCamera;
    
    private Player player => Global.Get().player;
    private bool wasThirdView;

    private SpatialCache cutsceneCameraCache;
    private SpatialCache playerCameraCache;
    
    public SpatialCache GetPlayerCameraData()
    {
        return playerCameraCache;
    }

    public Camera GetCamera()
    {
        return cutsceneCamera;
    }
    
    public void SetCameraParent(Node newParent)
    {
        var wasCameraEmpty = true;

        if (cutsceneCamera == null)
        {
            InitCamera();
        }
        else
        {
            wasCameraEmpty = false;
            cutsceneCameraCache = new SpatialCache
            (
                cutsceneCamera.GlobalTranslation, 
                cutsceneCamera.GlobalRotation
            );
        }

        if (cutsceneCamera.GetParent() != null)
        {
            cutsceneCamera.GetParent().RemoveChild(cutsceneCamera);
        }
        
        if (newParent != null) newParent.AddChild(cutsceneCamera);
        else AddChild(cutsceneCamera);

        if (wasCameraEmpty)
        {
            cutsceneCamera.GlobalTranslation = playerCameraCache.Pos;
            cutsceneCamera.GlobalRotation = playerCameraCache.Rot;
        }
        else
        {
            cutsceneCamera.GlobalTranslation = cutsceneCameraCache.Pos;
            cutsceneCamera.GlobalRotation = cutsceneCameraCache.Rot;
            cutsceneCameraCache = null;
        }
    }
    
    public async void ShowPlayerHead(float showPlayerHeadTimer)
    {
        if (showPlayerHeadTimer == 0) return;
        if (!wasThirdView) return;
        
        await Global.Get().ToTimer(showPlayerHeadTimer);
        
        player.RotationHelperThird.SetThirdView(true);
        player.RotationHelperThird.thirdCamera.Current = false;
        cutsceneCamera.Current = true;
    }

    public async void HidePlayerHead(float showPlayerHeadTimer)
    {
        if (showPlayerHeadTimer == 0) return;
        if (wasThirdView) return;
        
        await Global.Get().ToTimer(showPlayerHeadTimer);
        
        player.RotationHelperThird.SetThirdView(false);
        player.RotationHelperThird.firstCamera.Current = false;
        cutsceneCamera.Current = true;
    }

    public void ChangeCameraAngle(Vector3 angleInDegrees)
    {
        cutsceneCamera.Rotation = angleInDegrees * Mathf.Pi / 180;
    }
    
    public void ReturnPlayerCamera()
    {
        cutsceneCamera.Current = false;
        cutsceneCamera.QueueFree();
        cutsceneCamera = null;
        
        player.RotationHelperThird.SetThirdView(wasThirdView);
        player.SetMayMove(true);
        playerCameraCache = null;
    }
    
    private void InitCamera()
    {
        var prefab = GD.Load<PackedScene>("res://objects/cinematic/Camera.tscn");
        cutsceneCamera = prefab.Instance() as Camera;
        wasThirdView = player.ThirdView;
        
        var playerCamera = wasThirdView
            ? player.RotationHelperThird.thirdCamera
            : player.RotationHelperThird.firstCamera;

        playerCameraCache = new SpatialCache
        (
            playerCamera.GlobalTranslation, 
            playerCamera.GlobalRotation
        );
        
        player.SetMayMove(false);
        playerCamera.Current = false;
        cutsceneCamera.Current = true;
    }
}
