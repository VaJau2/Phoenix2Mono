using Godot;
using System;
using System.Collections.Generic;

public class Cutscene : Node
{
    private Camera cutsceneCamera;
    private Area area;
    
    private Player player => Global.Get().player;
    private bool wasThirdView;
    
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

    public Camera GetPlayerCamera()
    {
        return wasThirdView 
            ? player.RotationHelperThird.thirdCamera 
            : player.RotationHelperThird.firstCamera;
    }

    public Camera GetCamera()
    {
        return cutsceneCamera;
    }
    
    public void SetCameraParent(Node newParent)
    {
        SpatialCache cutsceneCameraCache;

        if (cutsceneCamera == null)
        {
            Init();
            cutsceneCameraCache = new SpatialCache
            (
                GetPlayerCamera().GlobalTranslation, 
                GetPlayerCamera().GlobalRotation
            );
        }
        else
        {
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

        cutsceneCamera.GlobalTranslation = cutsceneCameraCache.Pos;
        cutsceneCamera.GlobalRotation = cutsceneCameraCache.Rot;
    }

    public void AddQueue(CinematicTrigger trigger)
    {
        triggerQueue.Add(trigger);
        
        if (triggerQueue.Count == 1)
        {
            trigger.StartCinematics();
        }
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
        
        player.RotationHelperThird.SetThirdView(wasThirdView);
        player.Camera.isUpdating = true;
        player.MayRotateHead = true;
        player.SetMayMove(true);
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

        var headBone = player.Body.GetNode<BoneAttachment>("Armature/Skeleton/BoneAttachment");
        headBone.AddChild(area);
        
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
        
            await Global.Get().ToTimer(0.05f);
            triggerQueue[0].Skip();
        }
            
        subtitles.Skip();
        skipTrigger?._on_activate_trigger();

        await Global.Get().ToTimer(0.05f);
        
        if (cutsceneCamera != null) ReturnPlayerCamera();
    }
}
