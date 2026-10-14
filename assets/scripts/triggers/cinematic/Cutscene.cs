using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class Cutscene : Node, ISavable
{
    [Export] private bool changeListener;
    [Export] private bool DeleteAfterFinished;
    
    private Camera3D cutsceneCamera;
    private Area3D area;
    
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

    public Camera3D GetPlayerCamera()
    {
        return wasThirdView 
            ? player.RotationHelperThird.thirdCamera 
            : player.RotationHelperThird.firstCamera;
    }

    public Camera3D GetCamera()
    {
        return cutsceneCamera;
    }
    
    public void SetCameraParent(Node newParent)
    {
        Vector3 cachePos;
        Vector3 cacheRot;

        if (cutsceneCamera == null)
        {
            Init();
            cachePos = GetPlayerCamera().GlobalPosition;
            cacheRot = GetPlayerCamera().GlobalRotation;
        }
        else
        {
            cachePos = cutsceneCamera.GlobalPosition;
            cacheRot = cutsceneCamera.GlobalRotation;
        }

        if (cutsceneCamera.GetParent() != null)
        {
            cutsceneCamera.GetParent().RemoveChild(cutsceneCamera);
        }
        
        if (newParent != null) newParent.AddChild(cutsceneCamera);
        else AddChild(cutsceneCamera);

        cutsceneCamera.GlobalPosition = cachePos;
        cutsceneCamera.GlobalRotation = cacheRot;
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
        skip.Disconnect(nameof(global::Skip.SkipEvent), new Callable(this, nameof(Skip)));
        skip.SetActive(false);
        
        area.Disconnect("body_exited", this, nameof(ShowPlayerHead));
        area.Disconnect("body_entered", this, nameof(HidePlayerHead));
        area.QueueFree();
        
        player.RotationHelperThird.SetThirdView(wasThirdView);
        player.SetTotalMayMove(true);
        
        cutsceneCamera.QueueFree();
        cutsceneCamera = null;

        if (changeListener) player.MakeListenerCurrent();

        if (!DeleteAfterFinished) return;
        Global.AddDeletedObject(this);
        QueueFree();
    }

    private void Init()
    {
        InitCamera();
        InitHeadTrigger();
        if (changeListener) InitListener();
        skip.Connect(nameof(global::Skip.SkipEvent), new Callable(this, nameof(Skip)));
        skip.SetActive(true);
    }
    
    private void InitCamera()
    {
        var prefab = GD.Load<PackedScene>("res://objects/cinematic/Camera.tscn");
        
        if (prefab.Instantiate() is not Camera3D tempCamera)
        {
            throw new Exception("Camera prefab is not found");
        }
        
        cutsceneCamera = tempCamera;
        cutsceneCamera.Current = true;
        
        wasThirdView = player.ThirdView;
        player.SetTotalMayMove(false);
    }

    private void InitListener()
    {
        var listener = new AudioListener3D();
        cutsceneCamera.AddChild(listener);
        listener.MakeCurrent();
    }
    
    private void InitHeadTrigger()
    {
        var prefab = GD.Load<PackedScene>("res://objects/cinematic/HeadTrigger.tscn");
        
        if (prefab.Instantiate() is not Area3D tempArea)
        {
            throw new Exception("Area prefab is not found");
        }

        area = tempArea;

        var headBone = player.Body.GetNode<BoneAttachment3D>("Armature/Skeleton/BoneAttachment");
        headBone.AddChild(area);
        
        area.GlobalPosition = player.RotationHelperThird.firstCamera.GlobalPosition;
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

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        
        if (cutsceneCamera == null) return saveData;

        saveData["wasThirdView"] = wasThirdView;
        saveData["thirdView"] = player.ThirdView;
        saveData["cameraParentPath"] = cutsceneCamera.GetParent().GetPath();
        saveData["cameraPos"] = cutsceneCamera.GlobalPosition;
        saveData["cameraRot"] = cutsceneCamera.GlobalRotation;
        saveData["queueSize"] = triggerQueue.Count;
        
        if (triggerQueue.Count == 0) return saveData;

        for (var i = 0; i < triggerQueue.Count; i++)
        {
            saveData[$"trigger{i}"] = triggerQueue[i].GetPath();
        }

        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        if (!data.Contains("wasThirdView")) return;
        
        var cameraParentPath = Convert.ToString(data["cameraParentPath"]);
        var cameraParent = GetNode(cameraParentPath);
        
        Init();
        if (cameraParent == null) AddChild(cutsceneCamera);
        else cameraParent.AddChild(cutsceneCamera);

        cutsceneCamera.GlobalPosition = data["cameraPos"].ToString().ParseToVector3();
        cutsceneCamera.GlobalRotation = data["cameraRot"].ToString().ParseToVector3();

        wasThirdView = Convert.ToBoolean(data["wasThirdView"]);
        if (Convert.ToBoolean(data["thirdView"]))
        {
            player.RotationHelperThird.SetThirdView(true);
            player.RotationHelperThird.thirdCamera.Current = false;
            cutsceneCamera.Current = true;
        }
        
        var queueSize = Convert.ToInt32(data["queueSize"]);
        if (queueSize == 0) return;

        for (var i = 0; i < queueSize; i++)
        {
            var path = data[$"trigger{i}"].ToString();
            var trigger = GetNode<CinematicTrigger>(path);
            triggerQueue.Add(trigger);
        }
    }
}
