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

    private Array<NpcWithWeapons> npc = new Array<NpcWithWeapons>();
    private Array<Spatial> points = new Array<Spatial>();
    private bool activated;

    private float tempTimer = 0;
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

        SetProcess(false);
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
        
        if (step < npc.Count + 1)
        {
            WaitLastTimer();
            return;
        }
        
        base._on_activate_trigger();
    }

    private void LoadNpcAndPoints()
    {
        for (int i = 0; i < NpcPaths.Count; i++)
        {
            npc.Add(GetNode<NpcWithWeapons>(NpcPaths[i]));
            points.Add(GetNode<Spatial>(pointPaths[i]));
        }
        
        activated = true;
    }

    private void SetNpcAndWait()
    {
        int i = step;
        if (IsInstanceValid(npc[i]))
        {
            npc[i].SetNewStartPos(points[i].GlobalTransform.origin, runToPoint);
            npc[i].myStartRot = points[i].Rotation;
            if (idleAnims != null && idleAnims.Count > i)
            {
                npc[i].IdleAnim = idleAnims[i];
            }

            if (stayThere != null && stayThere.Count == npc.Count && npc[i] is Pony pony)
            {
                pony.stayInPoint = stayThere[i];
            }

            if (teleportToPoint)
            {
                Vector3 oldScale = npc[i].Scale;
                npc[i].GlobalTransform = Global.setNewOrigin(npc[i].GlobalTransform, points[i].GlobalTransform.origin);
                npc[i].Scale = oldScale;
            }
            
        }
        
        step++;
        tempTimer = timer;
        SetProcess(true);
    }

    private void WaitLastTimer()
    {
        step++;
        tempTimer = lastTimer;
        SetProcess(true);
    }
    
    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["step"] = step;
        saveData["tempTimer"] = tempTimer;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        
        if (data.Contains("tempTimer"))
        {
            tempTimer = Convert.ToSingle(data["tempTimer"]);
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
}