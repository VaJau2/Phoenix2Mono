using System;
using Godot;
using Godot.Collections;

public class NavigationMovingController: BaseMovingController, ISavable
{
    private const float RUN_DISTANCE = 12f;
    private const float MIN_MOVING_DISTANCE = 0.5f;
    private const float CHECK_MOVABLE_TIME = 6f;

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
    
    private float checkMovableTimer;
    private Vector3 oldCharacterPosition;
    private float customFinalDistance;
    private float oldComeDistance;
    private float oldRotationSpeed;
    
    private Vector3[] path;
    private int pathI;
    
    public void SetDoorWait(float value)
    {
        doorWait = value;
    }
    
    public void GoTo(Vector3 place, float distance = 0, bool mayRun = true)
    {
        if (customFinalDistance != 0) distance = customFinalDistance;
        else if (distance == 0) distance = ComeDistance;
        
        if (character.BaseSpeed == 0 || (!character.MayMove && character is not Player))
        {
            FinishGoingTo();
            return;
        }
        
        var pos = character.GlobalTranslation;
        var tempDistance = pos.DistanceTo(place);

        if (tempDistance > distance)
        {
            cameToPlace = false;
        }
        else
        {
            FinishGoingTo();
            return;
        }
        
        if (stopAreaEntered)
        {
            Stop(true);
            return;
        }
        
        CheckMovablePath();

        if (path == null)
        {
            pathI = 0;
            path = NavigationServer.MapGetPath(
                character.GetWorld().NavigationMap, 
                pos, 
                place, 
                true
            );
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

    private  void CheckMovablePath()
    {
        if (path == null) return;
        if (character.Velocity.Length() <= 0) return;

        if (checkMovableTimer > 0)
        {
            checkMovableTimer -= 0.1f;
        }
        else
        {
            if (oldCharacterPosition == Vector3.Zero)
            {
                oldCharacterPosition = character.GlobalTranslation;
            }
            else
            {
                var newPosition = character.GlobalTranslation;
                if (newPosition.DistanceTo(oldCharacterPosition) < MIN_MOVING_DISTANCE)
                {
                    //Делаем персонажа резким как пуля
                    //Чтобы он обходил все препятствия в случае застревания 
                    oldComeDistance = ComeDistance;
                    ComeDistance = 1f;
                    oldRotationSpeed = RotationSpeed;
                    RotationSpeed = 0.9f;
                    customFinalDistance = 3f;
                    path = null;
                }

                oldCharacterPosition = Vector3.Zero;
            }

            checkMovableTimer = CHECK_MOVABLE_TIME;
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
        if (!character.IsOnFloor())
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
        if (cameToPlace) return;
        
        customFinalDistance = 0;
        
        if (oldComeDistance != 0)
        {
            ComeDistance = oldComeDistance;
            oldComeDistance = 0;
        }

        if (oldRotationSpeed != 0)
        {
            RotationSpeed = oldRotationSpeed;
            oldRotationSpeed = 0;
        }
        
        Stop();
        cameToPlace = true;
        character.EmitSignal(nameof(Character.IsCame));
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
            MoveTo(path[pathI], ComeDistance, character.BaseSpeed);
            IsRunning = false;
        }
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["runSpeed"] = RunSpeed;
        saveData["runToPoint"] = RunToPoint;
        return saveData;
    }

    public void LoadData(Dictionary data)
    {
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