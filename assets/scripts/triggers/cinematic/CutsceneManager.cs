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
    
    public void SetCameraParent(Node parent)
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
            GD.Print("camera old pos: " + cutsceneCamera.GlobalTranslation);
            cutsceneCamera.GetParent().RemoveChild(cutsceneCamera);
        }
        
        if (parent != null) parent.AddChild(cutsceneCamera);
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
        
        GD.Print("camera new pos: " + cutsceneCamera.GlobalTranslation + '\n');
    }

    public void ChangeCameraAngle(Vector3 angleInDegrees)
    {
        cutsceneCamera.Rotation = angleInDegrees * Mathf.Pi / 180;
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
            
        if (!player.ThirdView)
        {
            player.RotationHelperThird.SetThirdView(true);
        }

        player.SetMayMove(false);
        playerCamera.Current = false;
        cutsceneCamera.Current = true;
    }

    public void ReturnPlayerCamera()
    {
        player?.RotationHelperThird.SetThirdView(wasThirdView);
        player?.SetMayMove(true);
        playerCameraCache = null;
        
        cutsceneCamera.Current = false;
        cutsceneCamera.QueueFree();
        cutsceneCamera = null;
    }
}
