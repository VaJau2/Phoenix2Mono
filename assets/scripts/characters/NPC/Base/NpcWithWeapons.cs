using Godot;
using Godot.Collections;

public class NpcWithWeapons : NPC
{
    private readonly float[] UNCOVER_TIMER = {5f, 20f};
    private readonly float[] COVER_TIMER = {1f, 5f};

    [Export] public float COME_DISTANCE = 4f;

    [Export] public string weaponCode = "";
    [Export] public Array<string> itemCodes = new Array<string>();
    [Export] public Dictionary<string, int> ammoCount = new Dictionary<string, int>();

    private Character followTarget;

    private Navigation navigation;
    protected bool cameToPlace = false;
    private bool updatePath = false;
    private float updatePathTimer = 0;
    protected Vector3[] path;
    protected int pathI;

    protected SoundSteps soundSteps;
    public NPCWeapons weapons;
    private CoversManager covers;
    protected Cover tempCover;
    protected Vector3 tempCoverPlace;
    public float coverTimer = 0;

    private float shootCooldown = 0;
    protected float doorWait;
    public bool IsHidingInCover => tempCover != null;
    public bool InCover = false;
    protected bool stopAreaEntered;

    [Signal]
    public delegate void IsCame();

    private RandomNumberGenerator rand = new RandomNumberGenerator();
    
    private bool OnFloor() {return soundSteps.landMaterial != null; }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["followTarget"] = followTarget?.GetPath();
        saveData["weaponCode"] = weaponCode;
        return saveData;
    }

    public override async void LoadData(Dictionary data)
    {
        base.LoadData(data);
        if (data["weaponCode"] != null)
        {
            weaponCode = data["weaponCode"].ToString();
            weapons.LoadWeapon(this, weaponCode);
        }

        if (!data.Contains("followTarget") || data["followTarget"] == null) return;

        await ToSignal(GetTree(), "idle_frame");
        var newFollowTarget = GetNode<Character>(data["followTarget"].ToString());
        SetFollowTarget(newFollowTarget);
    }

    public override void SetState(NPCState newState)
    {
        if (Health <= 0)
        {
            return;
        }

        base.SetState(newState);
        if (!string.IsNullOrEmpty(weaponCode))
        {
            StopHidingInCover();
        }

        cameToPlace = false;
        path = null;

        switch (newState)
        {
            case NPCState.Idle:
                weapons.SetWeapon(false);
                break;

            case NPCState.Attack:
                seekArea.MakeAlliesAttack();
                weapons.SetWeapon(true);
                break;
        }
    }

    public virtual void SetFollowTarget(Character newTarget)
    {
        followTarget = newTarget;
    }

    public virtual Spatial GetWeaponParent(bool isPistol)
    {
        return null;
    }

    public virtual void SetNewStartPos(Vector3 newPos, bool run = false)
    {
        CleanPatrolArray();
        cameToPlace = false;
        myStartPos = newPos;
    }

    protected virtual void FinishGoingTo()
    {
        Stop();
        cameToPlace = true;
        EmitSignal(nameof(IsCame));
    }

    protected virtual void MoveToPoint(float tempDistance, bool mayRun)
    {
        MoveTo(path[pathI], COME_DISTANCE, WalkSpeed);
    }

    private void GoTo(Vector3 place, float distance, bool mayRun = true)
    {
        if (WalkSpeed == 0 || !MayMove)
        {
            FinishGoingTo();
            return;
        }
        
        cameToPlace = false;
        var pos = GlobalTransform.origin;

        var tempDistance = pos.DistanceTo(place);
        if (tempDistance < distance)
        {
            FinishGoingTo();
            return;
        }

        if (path == null)
        {
            path = navigation.GetSimplePath(pos, place, true);
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

    private void FindCover()
    {
        if (InCover)
        {
            coverTimer = rand.RandfRange(COVER_TIMER[0], COVER_TIMER[1]);
        }
        else
        {
            tempCover = covers.GetCover(this);
            if (tempCover != null)
            {
                tempCoverPlace = tempCover.GetFarPlace(tempVictim.GlobalTransform.origin);
                cameToPlace = false;
            }

            InCover = false;
            coverTimer = rand.RandfRange(COVER_TIMER[0], COVER_TIMER[1]);
        }
    }

    public void StopHidingInCover()
    {
        if (tempCover != null) covers.ReturnCover(tempCover);
        tempCover = null;
        coverTimer = rand.RandfRange(UNCOVER_TIMER[0], UNCOVER_TIMER[1]);
        InCover = false;
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        if (IsImmortal) return;

        base.TakeDamage(damager, damage, shapeID);
        coverTimer -= damage / 10;
        if (string.IsNullOrEmpty(weaponCode))
        {
            StopHidingInCover();
        }
    }

    protected override void AnimateDealth(Character killer, int shapeID)
    {
        if (itemCodes.Count > 0 || ammoCount.Count > 0)
        {
            SpawnItemsBag();
        }

        weapons.SetWeapon(false);
        base.AnimateDealth(killer, shapeID);
    }

    private void LookAtTarget(Vector3 target)
    {
        target.y = GlobalTransform.origin.y;
        LookAt(target, Vector3.Up);
    }

    protected virtual void LookAtTarget(bool rotateBody = true)
    {
        Vector3 victimPos = tempVictim.GlobalTransform.origin;
        LookAtTarget(victimPos);
    }

    protected virtual void PlayStopAnim()
    {
    }

    protected void Stop(bool MoveDown = false)
    {
        PlayStopAnim();
        path = null;
        pathI = 0;
        if (MayMove && MoveDown && !OnFloor() )
        {
            Velocity = new Vector3(0, -GRAVITY, 0);
        }
        else
        {
            Velocity = Vector3.Zero;
        }
    }

    protected void SpawnItemsBag()
    {
        var bagPrefab = GD.Load<PackedScene>("res://objects/props/furniture/bag.tscn");
        var tempBag = (FurnChest) bagPrefab.Instance();

        tempBag.itemCodes = itemCodes;
        tempBag.ammoCount = ammoCount;

        Node parent = GetNode("/root/Main/Scene");
        parent.AddChild(tempBag);

        tempBag.Name = "Created_" + tempBag.Name;
        tempBag.Translation = Translation;
        tempBag.Translate(Vector3.Up / 4f);
    }

    protected void UpdatePath(float delta)
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

    protected void UpdateShooting(float victimDistance, float delta)
    {
        if (shootCooldown > 0)
        {
            shootCooldown -= delta;
        }
        else
        {
            shootCooldown = weapons.MakeShoot(victimDistance);
            coverTimer *= 0.75f;
        }
    }

    protected void AttackEnemy(float delta)
    {
        if (string.IsNullOrEmpty(weaponCode))
        {
            //если нет оружия
            //бегаем по укрытиям и молимся Селестии
            coverTimer = 0;
            return;
        }

        Vector3 victimPos = tempVictim.GlobalTransform.origin;
        float shootDistance = weapons.GetStatsFloat("shootDistance");
        float tempDistance = GlobalTransform.origin.DistanceTo(victimPos);
        if (cameToPlace && tempDistance < shootDistance)
        {
            UpdateShooting(tempDistance, delta);
            LookAtTarget();
            Stop(true);
        }
        else
        {
            GoTo(victimPos, shootDistance / 1.5f);
            updatePath = tempVictim.Velocity.Length() > 2;
        }
    }

    private void FollowTarget()
    {
        Vector3 targetPos = followTarget.GlobalTransform.origin;
        GoTo(targetPos, COME_DISTANCE * 2f);
        updatePath = followTarget?.Velocity.Length() > 2;
    }

    protected virtual void PlayIdleAnim()
    {
    }

    protected void SetDoorWait(float value)
    {
        doorWait = value;
    }

    protected void UpdateAI(float delta)
    {
        if (doorWait > 0)
        {
            doorWait -= delta;
            Stop(true);
            return;
        }
        
        switch (state)
        {
            case NPCState.Idle:
                if (followTarget != null)
                {
                    FollowTarget();
                }
                else if (patrolPoints == null || patrolPoints.Length == 0)
                {
                    if (!cameToPlace)
                    {
                        if (stopAreaEntered)
                        {
                            Stop(true);
                        }
                        else
                        {
                            GoTo(myStartPos, COME_DISTANCE, false);
                        }
                    }
                    else
                    {
                        GlobalTransform = Global.setNewOrigin(GlobalTransform, myStartPos);
                        Rotation = new Vector3(
                            Rotation.x,
                            myStartRot.y,
                            Rotation.z
                        );
                        PlayIdleAnim();
                    }
                }
                else
                {
                    if (patrolWaitTimer > 0)
                    {
                        patrolWaitTimer -= delta;
                    }
                    else
                    {
                        if (stopAreaEntered)
                        {
                            Stop(true);
                        }
                        else
                        {
                            GoTo(patrolPoints[patrolI].GlobalTransform.origin, COME_DISTANCE, false);
                        }

                        if (cameToPlace)
                        {
                            if (patrolI < patrolPoints.Length - 1)
                            {
                                patrolI += 1;
                            }
                            else
                            {
                                patrolI = 0;
                            }

                            patrolWaitTimer = PATROL_WAIT;
                        }
                    }
                }

                break;
            case NPCState.Attack:

                if (tempVictim.Health <= 0)
                {
                    SetState(NPCState.Idle);
                    return;
                }

                if (coverTimer > 0)
                {
                    coverTimer -= delta;
                    if (tempCover == null)
                    {
                        //идем в атаку
                        AttackEnemy(delta);
                    }
                    else
                    {
                        //прячемся в укрытии
                        if (!cameToPlace)
                        {
                            GoTo(tempCoverPlace, COME_DISTANCE);
                        }
                        else
                        {
                            LookAtTarget();
                            InCover = true;
                        }
                    }
                }
                else
                {
                    if (tempCover == null)
                    {
                        FindCover();
                    }
                    else if (!string.IsNullOrEmpty(weaponCode))
                    {
                        StopHidingInCover();
                    }
                }

                break;
            case NPCState.Search:
                if (searchTimer > 0 && !string.IsNullOrEmpty(weaponCode))
                {
                    searchTimer -= delta;
                }
                else
                {
                    SetState(NPCState.Idle);
                    return;
                }
                
                if (WalkSpeed == 0)
                {
                    LookAtTarget(lastSeePos);
                }
                else if (!cameToPlace)
                {
                    GoTo(lastSeePos, COME_DISTANCE);
                }

                break;

            case NPCState.Talk:
                Stop();
                LookAtTarget(false);
                break;
        }
    }

    public override void _Ready()
    {
        navigation = GetNode<Navigation>("/root/Main/Scene/Navigation");
        covers = GetNode<CoversManager>("/root/Main/Scene/terrain/covers");
        weapons = GetNode<NPCWeapons>("weapons");
        soundSteps = GetNode<SoundSteps>("Armature/Skeleton/floorRay");
        if (weaponCode != "")
        {
            weapons.LoadWeapon(this, weaponCode);
        }

        base._Ready();
    }
}