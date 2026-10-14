using System;
using Godot;
using Godot.Collections;

public partial class MoveNpcTrigger: ActivateOtherTrigger
{
    [Export] public Array<string> NpcPaths; 
    [Export] public Array<NodePath> pointPaths;
    [Export] public Array<string> idleAnims;
    [Export] private Array<bool> stayThere;
    [Export] public float timer = 1f;
    [Export] public float lastTimer = 1f;
    [Export] public bool runToPoint;
    [Export] public bool teleportToPoint;
    [Export] public bool setStartPoint = true;

    private Array<NPC> npc = [];
    private Array<Node3D> points = [];
    private bool activated;
    private Array<int> connectedEvents = [];
    private float tempTimer;
    private int step;

    public override void _Ready()
    {
        base._Ready();
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= (float)delta;
            return;
        }

        if (step <= npc.Count)
        {
            SetProcess(false);
        }
        
        _on_activate_trigger();
    }


    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (activated) return;
        _on_activate_trigger();
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        if (NpcPaths == null || pointPaths == null) return;

        if (npc.Count == 0 || points.Count == 0)
        {
            LoadNpcAndPoints();
        }
        
        if (step < npc.Count)
        {
            SetNpcAndWait();
            return;
        }
        
        if (step == npc.Count)
        {
            WaitLastTimer();
            return;
        }

        if (connectedEvents.Count == 0)
        {
            base._on_activate_trigger();
        }
    }

    private void LoadNpcAndPoints()
    {
        for (int i = 0; i < NpcPaths.Count; i++)
        {
            npc.Add(GetNode<NPC>(NpcPaths[i]));
            points.Add(GetNode<Node3D>(pointPaths[i]));
        }
        
        activated = true;
    }

    private void SetNpcAndWait()
    {
        int i = step;
        if (IsInstanceValid(npc[i]))
        {
            if (setStartPoint)
            {
                npc[i].SetNewStartPos(points[i].GlobalTransform.Origin, runToPoint);
                npc[i].myStartRot = points[i].Rotation;
            }
            
            if (idleAnims != null && idleAnims.Count > i)
            {
                BoundCall.Connect(npc[i], Character.SignalName.IsCame, this, nameof(AfterNpcCameToPoint), [i]);
                connectedEvents.Add(i);
            }

            if (teleportToPoint)
            {
                npc[i].GlobalTransform = Global.SetNewOrigin(npc[i].GlobalTransform, points[i].GlobalTransform.Origin);
            }
        }
        
        step++;
        tempTimer = timer;
        SetProcess(true);
    }
    
    public void AfterNpcCameToPoint(int i)
    {
        if (stayThere.Count > i)
        {
            if (stayThere[i])
            {
                npc[i].SetState(SetStateEnum.Idle);
            }
        
            npc[i].MayChangeState = !stayThere[i];
        }
        
        var body = npc[i].GetNodeOrNull<PonyBody>("body");
        if (body != null)
        {
            body.CustomIdleAnim = idleAnims.Count > i 
                ? idleAnims[i] 
                : null;
        }

        if (connectedEvents.Contains(i))
        {
            BoundCall.Disconnect(npc[i], Character.SignalName.IsCame, this, nameof(AfterNpcCameToPoint));
            connectedEvents.Remove(i);
            _on_activate_trigger();
        }
    }
    
    private void WaitLastTimer()
    {
        NextStep(lastTimer);
    }

    private void NextStep(float timerToWait)
    {
        step++;
        tempTimer = timerToWait;
        SetProcess(true);
    }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["step"] = step;
        saveData["tempTimer"] = tempTimer;
        saveData["connectedEvents"] = ConvertUtils.ArrayToString(connectedEvents);
        
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        
        if (data.ContainsKey("tempTimer"))
        {
            tempTimer = data["tempTimer"].AsSingle();
        }
        
        if (data.ContainsKey("connectedEvents"))
        {
            connectedEvents = ConvertUtils.StringToArray(data["connectedEvents"].ToString());
            
            if (connectedEvents.Count > 0)
            {
                LoadNpcAndPoints();
            }
            
            foreach (var connectedId in connectedEvents)
            {
                var isConnected = BoundCall.IsConnected(npc[connectedId], Character.SignalName.IsCame, this, nameof(AfterNpcCameToPoint));
                
                if (isConnected) continue;
                
                BoundCall.Connect(npc[connectedId], Character.SignalName.IsCame, this, nameof(AfterNpcCameToPoint), [connectedId]);
            }
        }
        
        step = data["step"].AsInt32();
        if (step > 0)
        {
            _on_activate_trigger();
        }
    }
    
    public override void _on_body_entered(Node body)
    {
        if (activated) return;
        if (!IsActive) return;
        if (!(body is Player)) return;
        _on_activate_trigger();
    }
}