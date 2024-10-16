using Godot;
using System;
using System.Collections.Generic;

public class Cutscene : Node
{
    private Camera cutsceneCamera;
    private Area area;
    
    private Player player => Global.Get().player;
    private bool wasThirdView;

    private SpatialCache cutsceneCameraCache;
    private SpatialCache playerCameraCache;
    
    private readonly List<CinematicTrigger> triggerQueue = [];
    
    private Skip skip;
    private TriggerBase skipTrigger;
    private Subtitles subtitles;
    
    public override void _Ready()
    {
        skip = GetNode<Skip>("/root/Main/Scene/canvas/skip");
        skipTrigger = GetNodeOrNull<TriggerBase>("skip");
        subtitles = GetNode<Subtitles>("/root/Main/Scene/canvas/subtitles");
    }

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
            Init();
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

    public void AddQueue(CinematicTrigger trigger)
    {
        if (triggerQueue.Count == 0)
        {
            trigger.StartCinematics();
        }
        
        triggerQueue.Add(trigger);
    }

    public void OnTriggerFinished()
    {
        triggerQueue.RemoveAt(0);
        
        if (triggerQueue.Count > 0)
        {
            triggerQueue[0].StartCinematics();
        }
    }
    
    public void ReturnPlayerCamera()
    {
        skip.Disconnect(nameof(global::Skip.SkipEvent), this, nameof(Skip));
        skip.SetActive(false);
        
        area.Disconnect("body_exited", this, nameof(ShowPlayerHead));
        area.Disconnect("body_entered", this, nameof(HidePlayerHead));
        area.QueueFree();
        
        cutsceneCamera.QueueFree();
        cutsceneCamera = null;
        skipTrigger = null;
        
        player.RotationHelperThird.SetThirdView(wasThirdView);
        player.Camera.isUpdating = true;
        player.MayRotateHead = true;
        player.SetMayMove(true);
        playerCameraCache = null;
    }

    private void Init()
    {
        InitCamera();
        InitHeadTrigger();
        skip.Connect(nameof(global::Skip.SkipEvent), this, nameof(Skip));
        skip.SetActive(true);
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

    private async void Skip()
    {
        if (cutsceneCamera == null) return; 
        
        if (triggerQueue.Count > 0)
        {
            if (triggerQueue.Count > 1)
            {
                triggerQueue.RemoveRange(1, triggerQueue.Count - 1);
            }
        
            triggerQueue[0].Skip();
        }
            
        subtitles.Skip();
        skipTrigger?._on_activate_trigger();

        await Global.Get().ToTimer(0.05f);
        
        ReturnPlayerCamera();
    }
}
