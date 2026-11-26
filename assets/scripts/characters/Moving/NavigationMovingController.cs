using System;
using Godot;
using Godot.Collections;

public partial class NavigationMovingController: BaseMovingController, ISavable
{
    private const float RUN_DISTANCE = 12f;
    private const float MIN_MOVING_DISTANCE = 0.5f;
    private const float CHECK_MOVABLE_TIME = 1f;           //секунды между проверками застревания

    //режим «пули» при застревании: точнее проходит точки пути и резче поворачивает
    private const float UNSTUCK_COME_DISTANCE = 1f;
    private const float UNSTUCK_ROTATION_SPEED = 0.9f;
    private const float UNSTUCK_FINAL_DISTANCE = 3f;

    //обход препятствия, в которое NPC упёрся
    private const float DETOUR_LENGTH = 2.5f;
    private const float DETOUR_TIME = 1.5f;
    private const float DETOUR_COME_DISTANCE = 0.5f;
    private const float SAME_STUCK_PLACE_DISTANCE = 1f;

    //упёрся в стоящего персонажа ближе этого к цели — дошёл, пока тот не отойдёт
    private const float BLOCKED_ARRIVE_DISTANCE = 8f;
    private const float BLOCKER_LEFT_DISTANCE = 4f;

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
    
    private float checkMovableTimer = CHECK_MOVABLE_TIME;
    private Vector3? oldCharacterPosition;
    private float customFinalDistance;
    private bool unstuckMode;
    private float oldComeDistance;
    private float oldRotationSpeed;

    private Vector3? detourPoint;
    private float detourTimer;
    private float detourSign = 1f;
    private Vector3 lastStuckPosition;

    private bool hasBlockedTarget;
    private Vector3 blockedTarget;
    private float blockedArriveDistance;
    private Character blocker;

    //«дошёл», упёршись в стоящего у цели персонажа
    public bool IsBlockedNearTarget => hasBlockedTarget;

    private Vector3 tempTarget;
    private Vector3 pathLastPoint;
    private Vector3[] path;
    private int pathI;
    
    public void SetDoorWait(float value)
    {
        doorWait = value;
    }
    
    public void GoTo(Vector3 target, float distance = 0, bool mayRun = true)
    {
        if (customFinalDistance != 0) distance = customFinalDistance;
        else if (distance == 0) distance = ComeDistance;

        //упёрся в стоящего у цели персонажа (OnStuck) — пока цель та же, считается дошедшим
        if (hasBlockedTarget)
        {
            if (target.DistanceTo(blockedTarget) > TARGET_MOVED_DISTANCE || IsBlockerLeft()) hasBlockedTarget = false;
            else distance = Mathf.Max(distance, blockedArriveDistance);
        }

        if (character.BaseSpeed == 0 || (!character.MayMove && character is not Player))
        {
            FinishGoingTo();
            return;
        }
        
        var pos = character.GlobalPosition;
        var tempDistance = pos.DistanceTo(GetLastPoint(target));

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
        
        if (CheckMovablePath()) return;

        if (detourPoint is Vector3 detour)
        {
            detourTimer -= (float)GetProcessDeltaTime();
            MoveTo(detour, DETOUR_COME_DISTANCE, character.BaseSpeed);
            IsRunning = false;
            if (!CloseToPoint && detourTimer > 0) return;

            //обошли — дальше по новому пути от текущего места
            detourPoint = null;
            path = null;
        }

        if (path == null || IsTargetMoved(target))
        {
            pathI = 0;
            path = NavigationServer3D.MapGetPath(
                character.GetWorld3D().NavigationMap, 
                pos, 
                target, 
                true
            );
            //путь пуст, пока карта навигации не готова или точки вне навмеша — идём к цели напрямую
            if (path.Length == 0)
            {
                path = [target];
            }
            pathLastPoint = path[path.Length - 1];
            tempTarget = target;
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

    //путь пересчитывается, только если цель сместилась на метр: при пересчёте каждый кадр NPC не уходил
    //дальше path[0] (своей позиции) и стоял, пока цель двигалась (B-52)
    private const float TARGET_MOVED_DISTANCE = 1f;
    
    private bool IsTargetMoved(Vector3 target) => target.DistanceTo(tempTarget) > TARGET_MOVED_DISTANCE;

    private Vector3 GetLastPoint(Vector3 target)
    {
        if (target != tempTarget)
        {
            return target;
        }
        
        return pathLastPoint != Vector3.Zero ? pathLastPoint : target;
    }

    //раз в секунду проверяет, сдвинулся ли идущий персонаж (B-53); true — засчитан дошедшим, GoTo прервать
    private bool CheckMovablePath()
    {
        if (path == null || detourPoint != null) return false;
        if (character.Velocity.Length() <= 0) return false;

        checkMovableTimer -= (float)GetProcessDeltaTime();
        if (checkMovableTimer > 0) return false;
        checkMovableTimer = CHECK_MOVABLE_TIME;

        var position = character.GlobalPosition;
        var lastPosition = oldCharacterPosition;
        oldCharacterPosition = position;
        if (lastPosition == null) return false;

        if (position.DistanceTo(lastPosition.Value) >= MIN_MOVING_DISTANCE)
        {
            SetUnstuckMode(false);
            return false;
        }

        return OnStuck(position);
    }

    //новый путь совпал бы со старым, поэтому по препятствию: стоящий у цели персонаж — дошли, идущий — оба влево,
    //стена/мебель — обход вдоль неё к точке пути (при повторе на том же месте — в другую сторону)
    private bool OnStuck(Vector3 position)
    {
        var heading = path[Mathf.Min(pathI, path.Length - 1)];
        SetUnstuckMode(true);
        path = null;

        if (!TryGetObstacle(out var normal, out var collider)) return false;

        if (collider is Character other)
        {
            var otherSpeed = new Vector2(other.Velocity.X, other.Velocity.Z).Length();
            var targetDistance = position.DistanceTo(GetLastPoint(tempTarget));

            if (otherSpeed < Character.MIN_WALKING_SPEED && targetDistance < BLOCKED_ARRIVE_DISTANCE)
            {
                hasBlockedTarget = true;
                blockedTarget = tempTarget;
                blockedArriveDistance = targetDistance + 1f;
                blocker = other;
                FinishGoingTo();
                return true;
            }

            StartDetour(position, normal, 1f);
            return false;
        }

        var tangent = normal.Cross(Vector3.Up);
        var sign = tangent.Dot(heading - position) >= 0 ? 1f : -1f;
        if (position.DistanceTo(lastStuckPosition) < SAME_STUCK_PLACE_DISTANCE)
        {
            sign = -detourSign;
        }

        StartDetour(position, normal, sign);
        return false;
    }

    private bool IsBlockerLeft() =>
        !IsInstanceValid(blocker)
        || blocker.Health <= 0
        || blocker.GlobalPosition.DistanceTo(character.GlobalPosition) > BLOCKER_LEFT_DISTANCE;

    //препятствие впереди по последнему move_and_slide и его горизонтальная нормаль
    private bool TryGetObstacle(out Vector3 normal, out GodotObject collider)
    {
        normal = Vector3.Zero;
        collider = null;
        var horizontal = new Vector3(character.Velocity.X, 0, character.Velocity.Z);

        for (int i = 0; i < character.GetSlideCollisionCount(); i++)
        {
            var collision = character.GetSlideCollision(i);
            var tempNormal = collision.GetNormal();
            if (Mathf.Abs(tempNormal.Y) > 0.7f) continue;   //пол или потолок

            tempNormal = new Vector3(tempNormal.X, 0, tempNormal.Z).Normalized();
            if (tempNormal.Dot(horizontal) >= 0) continue;   //не впереди

            normal = tempNormal;
            collider = collision.GetCollider();
            if (collider is Character) return true;          //персонаж важнее стены
        }

        return collider != null;
    }

    private void StartDetour(Vector3 position, Vector3 normal, float sign)
    {
        var tangent = normal.Cross(Vector3.Up) * sign;
        var point = position + tangent * DETOUR_LENGTH + normal * DETOUR_COME_DISTANCE;
        var map = character.GetWorld3D().NavigationMap;
        var navPoint = NavigationServer3D.MapGetClosestPoint(map, point);

        detourPoint = navPoint != Vector3.Zero ? navPoint : point;
        detourTimer = DETOUR_TIME;
        detourSign = sign;
        lastStuckPosition = position;
    }

    private void SetUnstuckMode(bool on)
    {
        if (unstuckMode == on) return;
        unstuckMode = on;

        //раньше old* перезаписывались при повторном застревании значениями 1 и 0.9 — NPC навсегда оставался резким (B-53)
        if (on)
        {
            oldComeDistance = ComeDistance;
            oldRotationSpeed = RotationSpeed;
            ComeDistance = UNSTUCK_COME_DISTANCE;
            RotationSpeed = UNSTUCK_ROTATION_SPEED;
            customFinalDistance = UNSTUCK_FINAL_DISTANCE;
        }
        else
        {
            ComeDistance = oldComeDistance;
            RotationSpeed = oldRotationSpeed;
            customFinalDistance = 0;
        }
    }

    public override void Stop(bool moveDown = false)
    {
        path = null;
        pathI = 0;
        detourPoint = null;
        oldCharacterPosition = null;
        checkMovableTimer = CHECK_MOVABLE_TIME;
        SetUnstuckMode(false);
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
    
    public void FinishGoingTo()
    {
        if (cameToPlace) return;

        Stop();
        cameToPlace = true;
        character.EmitSignal(Character.SignalName.IsCame);
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
        RunSpeed = data["runSpeed"].AsInt16();
        RunToPoint = data["runToPoint"].AsBool();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        
        if (doorWait > 0)
        {
            doorWait -= (float)delta;
            Stop(true);
            return;
        }
        
        HandleGravity();
        UpdatePath((float)delta);
    }
}