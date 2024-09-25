using System;
using Godot;

public class CutsceneManager : Node
{
    private Camera cutsceneCamera;
    private Area area;
    
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
    
    public void ReturnPlayerCamera()
    {
        area.Disconnect("body_exited", this, nameof(ShowPlayerHead));
        area.Disconnect("body_entered", this, nameof(HidePlayerHead));
        area.QueueFree();
        
        cutsceneCamera.QueueFree();
        cutsceneCamera = null;
        
        player.RotationHelperThird.SetThirdView(wasThirdView);
        player.Camera.isUpdating = true;
        player.MayRotateHead = true;
        player.SetMayMove(true);
        playerCameraCache = null;
    }
    
    private void InitCamera()
    {
        var prefab = GD.Load<PackedScene>("res://objects/cinematic/Camera.tscn");
        
        if (prefab.Instance() is not Camera tempCamera)
        {
            throw new Exception("Camera prefab is not found");
        }
        
        cutsceneCamera = tempCamera;
        cutsceneCamera.Current = true;
        
        wasThirdView = player.ThirdView;
        var playerCamera = player.RotationHelperThird.GetCamera();
        
        playerCameraCache = new SpatialCache
        (
            playerCamera.GlobalTranslation, 
            playerCamera.GlobalRotation
        );
        
        player.SetMayMove(false);
        player.MayRotateHead = false;
        player.Camera.isUpdating = false;
        InitHeadTrigger();
    }

    private void InitHeadTrigger()
    {
        var prefab = GD.Load<PackedScene>("res://objects/cinematic/HeadTrigger.tscn");
        
        if (prefab.Instance() is not Area tempArea)
        {
            throw new Exception("Area prefab is not found");
        }

        area = tempArea;
        AddChild(area);
        area.GlobalTranslation = player.RotationHelperThird.firstCamera.GlobalTranslation;
        area.Connect("body_exited", this, nameof(ShowPlayerHead));
        area.Connect("body_entered", this, nameof(HidePlayerHead));
    }
    
    private void ShowPlayerHead(Node body)
    {
        if (body.Name != "CinematicBody") return;
        if (wasThirdView) return;
        
        player.RotationHelperThird.SetThirdView(true);
        player.RotationHelperThird.thirdCamera.Current = false;
        cutsceneCamera.Current = true;
    }

    private void HidePlayerHead(Node body)
    {
        if (body.Name != "CinematicBody") return;
        if (wasThirdView) return;
        
        player.RotationHelperThird.SetThirdView(false);
        player.RotationHelperThird.firstCamera.Current = false;
        cutsceneCamera.Current = true;
    }
}
