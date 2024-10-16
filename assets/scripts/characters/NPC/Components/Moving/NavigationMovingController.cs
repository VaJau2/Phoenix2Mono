using System;
using Godot;
using Godot.Collections;

public class NavigationMovingController: BaseMovingController, ISavable
{
    private const float RUN_DISTANCE = 12f;

    [Export] public float ComeDistance = 3f;
    [Export] public bool MayRun = true;
    [Export] public int RunSpeed = 10;

    public bool RunToPoint;
    public bool IsRunning;
    public bool updatePath;
    public bool cameToPlace;
    public bool stopAreaEntered;
    
    private float doorWait;
    private float updatePathTimer;
    
    private Vector3[] path;
    private int pathI;
    
    public void SetDoorWait(float value)
    {
        doorWait = value;
    }
    
    public void GoTo(Vector3 place, float distance = 0, bool mayRun = true)
    {
        if (distance == 0) distance = ComeDistance;
        
        if (BaseSpeed == 0 || !Npc.MayMove)
        {
            FinishGoingTo();
            return;
        }
        
        cameToPlace = false;
        var pos = Npc.GlobalTranslation;

        var tempDistance = pos.DistanceTo(place);
        if (tempDistance < distance)
        {
            FinishGoingTo();
            return;
        }

        if (path == null)
        {
            path = NavigationServer.MapGetPath(Npc.GetWorld().NavigationMap, pos, place, true);
            pathI = 0;
        }

        if (path.Length == 0)
        {
            Stop();
            return;
        }

        MoveToPoint(tempDistance, mayRun);

        if (CloseToPoint)
        {
            if (pathI < path.Length - 1)
            {
                pathI += 1;
            }
            else
            {
                FinishGoingTo();
            }
        }
    }

    public override void Stop(bool moveDown = false)
    {
        path = null;
        pathI = 0;
        base.Stop(moveDown);
    }
    
    private void HandleGravity()
    {
        if (!Npc.IsOnFloor())
        {
            if (Gravity < 30)
            {
                Gravity += 0.5f;
            }
        }
        else
        {
            Gravity = 0;
        }
    }
    
    private void FinishGoingTo()
    {
        Stop();
        cameToPlace = true;
        Npc.EmitSignal(nameof(NPC.IsCame));
    }
    
    private void UpdatePath(float delta)
    {
        if (updatePath)
        {
            if (updatePathTimer > 0)
            {
                updatePathTimer -= delta;
            }
            else
            {
                path = null;
                updatePathTimer = 1;
                updatePath = false;
            }
        }
    }
    
    private void MoveToPoint(float tempDistance, bool mayRun)
    {
        if (MayRun && mayRun && tempDistance > RUN_DISTANCE) 
        {
            MoveTo(path[pathI], ComeDistance, RunSpeed);
            IsRunning = true;
        } 
        else 
        {
            MoveTo(path[pathI], ComeDistance, BaseSpeed);
            IsRunning = false;
        }
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["walkSpeed"] = BaseSpeed;
        saveData["runSpeed"] = RunSpeed;
        saveData["runToPoint"] = RunToPoint;
        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        BaseSpeed = Convert.ToInt16(data["walkSpeed"]);
        RunSpeed = Convert.ToInt16(data["runSpeed"]);
        RunToPoint = Convert.ToBoolean(data["runToPoint"]);
    }

    public override void _Process(float delta)
    {
        base._Process(delta);
        
        if (doorWait > 0)
        {
            doorWait -= delta;
            Stop(true);
            return;
        }
        
        HandleGravity();
        UpdatePath(delta);
    }
}