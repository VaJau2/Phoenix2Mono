using System;
using Godot;
using Godot.Collections;

public class MoveNpcTrigger: ActivateOtherTrigger
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
    private Array<Spatial> points = [];
    private bool activated;
    private Array<int> connectedEvents = [];
    private float tempTimer;
    private int step;

    public override void _Ready()
    {
        base._Ready();
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= delta;
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
            points.Add(GetNode<Spatial>(pointPaths[i]));
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
                npc[i].SetNewStartPos(points[i].GlobalTransform.origin, runToPoint);
                npc[i].myStartRot = points[i].Rotation;
            }
            
            if (idleAnims != null && idleAnims.Count > i)
            {
                npc[i].Connect(nameof(Character.IsCame), this, nameof(AfterNpcCameToPoint), [i]);
                connectedEvents.Add(i);
            }

            if (teleportToPoint)
            {
                npc[i].GlobalTransform = Global.SetNewOrigin(npc[i].GlobalTransform, points[i].GlobalTransform.origin);
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
            npc[i].Disconnect(nameof(Character.IsCame), this, nameof(AfterNpcCameToPoint));
            connectedEvents.Remove(i);
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
        saveData["connectedEvents"] = ArrayToString(connectedEvents);
        
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        
        if (data.Contains("tempTimer"))
        {
            tempTimer = Convert.ToSingle(data["tempTimer"]);
        }
        
        if (data.Contains("connectedEvents"))
        {
            connectedEvents = StringToArray(data["connectedEvents"].ToString());
            
            if (connectedEvents.Count > 0)
            {
                LoadNpcAndPoints();
            }
            
            foreach (var connectedId in connectedEvents)
            {
                npc[connectedId].Connect(
                    nameof(Character.IsCame),
                    this,
                    nameof(AfterNpcCameToPoint),
                    [connectedId]
                );
            }
        }
        
        step = Convert.ToInt16(data["step"]);
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

    private static string ArrayToString(Array<int> value)
    {
        if (value.Count <= 0) return "";
        var eventsString = "";

        for (var i = 0; i < value.Count; i++)
        {
            eventsString += value[i];
            if (i < value.Count - 1)
            {
                eventsString += ",";
            }
        }
            
        return eventsString;
    }

    private static Array<int> StringToArray(string value)
    {
        if (string.IsNullOrEmpty(value)) return [];

        var connect = value.Split(',');
        
        Array<int> result = [];

        foreach (var item in connect)
        {
            result.Add(int.Parse(item));
        }
        
        return result;
    }
}