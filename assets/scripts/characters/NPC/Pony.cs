using Godot;
using Godot.Collections;

//Класс для пней-неписей
//Также включает в себя единорогов, которые умеют только в левитацию
public class Pony: NPC
{
    const float RUN_DISTANCE = 12f;
    public const float COME_DISTANCE = 3.5f;

    private readonly float[] UNCOVER_TIMER = {5f, 20f};
    private readonly float[] COVER_TIMER = {1f, 5f};

    [Export]
    public int RunSpeed = 10;
    [Export]
    public string weaponCode = "";
    [Export]
    public Array<string> itemCodes = new Array<string>();
    [Export]
    public Dictionary<string, int> ammoCount = new Dictionary<string, int>();

    [Export]
    public bool IsUnicorn = false;
    public Particles MagicParticles;

    public bool IsRunning = false;
    public float RotationToVictim = 0f;

    public NPCFace head;
    public NPCBody body;
    public NPCWeapons weapons;
    private PackedScene bagPrefab;

    private Navigation navigation;
    private bool cameToPlace = false;
    private bool updatePath = false;
    private float updatePathTimer = 0;
    private Vector3[] path;
    private int pathI;

    private CoversManager covers;
    private Cover tempCover;
    private Vector3 tempCoverPlace;
    public float coverTimer = 0;

    private float doorWait = 0;
    private float shootCooldown = 0;

    private RandomNumberGenerator rand = new RandomNumberGenerator();

    [Signal]
    public delegate void IsCame();

    public bool IsHidingInCover => tempCover != null;
    public bool InCover = false;

    public override void SetState(NPCState newState)
    {
        if (Health <= 0) {
            return;
        }

        base.SetState(newState);
        cameToPlace = false;
        path = null;

        switch(newState) {
            case NPCState.Idle:
                body.lookTarget = null;
                weapons.SetWeapon(false);
                break;
            
            case NPCState.Attack:
                StopHidingInCover();
                seekArea.MakeAlliesAttack();
                body.lookTarget = tempVictim;
                weapons.SetWeapon(true);
                break;
            
            case NPCState.Search:
                body.lookTarget = null;
                break;
        }
    }
    
    public Spatial GetWeaponParent(bool isPistol)
    {
        if (IsUnicorn) {
            return GetNode<Spatial>("levitation/weapons");
        }
        else if (isPistol) {
            return GetNode<Spatial>("Armature/Skeleton/BoneAttachment/weapons");
        } else {
            return GetNode<Spatial>("Armature/Skeleton/BoneAttachment 2/weapons");
        }
    }

    public void SetNewStartPos(Vector3 newPos)
    {
        cameToPlace = false;
        myStartPos = newPos;
    }

    private void FinishGoingTo()
    {
        Stop();
        cameToPlace = true;
        EmitSignal(nameof(IsCame));
    }

    public void GoTo(Vector3 place, float distance = COME_DISTANCE, bool mayRun = true)
    {
        cameToPlace = false;
        var pos = GlobalTransform.origin;

        var tempDistance = pos.DistanceTo(place);
        if (tempDistance < distance) {
            FinishGoingTo();
            return;
        }

        if (doorWait > 0) {
            doorWait -= 0.05f;
            Stop();
            return;
        }

        if (path == null) {
            path = navigation.GetSimplePath(pos, place, true);
            pathI = 0;
        }

        if (path.Length == 0) {
            Stop();
            return;
        }

        if (mayRun && tempDistance > RUN_DISTANCE) {
            body.PlayAnim("Run");
            MoveTo(path[pathI], COME_DISTANCE, RunSpeed);
            IsRunning = true;
        } else {
            body.PlayAnim("Walk");
            MoveTo(path[pathI], COME_DISTANCE, WalkSpeed);
            IsRunning = false;
        }

        if (CloseToPoint) {
            if (pathI < path.Length - 1) {
                pathI += 1;
            } else {
                FinishGoingTo();
            }
        }
    }

    public void FindCover()
    {
        tempCover = covers.GetCover(this);
        if (tempCover != null) {
            tempCoverPlace = tempCover.GetFarPlace(tempVictim.GlobalTransform.origin);
            cameToPlace = false;
        }
        coverTimer = rand.RandfRange(COVER_TIMER[0], COVER_TIMER[1]);
        InCover = false;
    }

    public void StopHidingInCover()
    {
        if (tempCover != null) covers.ReturnCover(tempCover);
        tempCover = null;
        coverTimer = rand.RandfRange(UNCOVER_TIMER[0], UNCOVER_TIMER[1]);
    }
    
    private void Stop(bool MoveDown = false)
    {
        if (Velocity.Length() > 0) {
            body.PlayAnim("Idle1");
            path = null;
            pathI = 0;
            if (MoveDown) {
                Velocity = new Vector3(0, -GRAVITY, 0);
            } else {
                Velocity = Vector3.Zero;
            }
        }
    }

    private void LookAtTarget(bool rotateBody = true)
    {
        if (tempVictim == null) {
            RotationToVictim = 0;
            return;
        }

        var npcForward = -GlobalTransform.basis.z;
        var npcDir = body.GetDirToTarget(tempVictim);
        RotationToVictim = npcForward.AngleTo(npcDir);

        if (weapons.isPistol || !rotateBody) {
            if (Mathf.Rad2Deg(RotationToVictim) < 80) {
                return;
            }
        }

        Vector3 victimPos = tempVictim.GlobalTransform.origin;
        victimPos.y = GlobalTransform.origin.y;
        LookAt(victimPos, Vector3.Up);
    }

    private void SpawnItemsBag()
    {
        FurnChest tempBag = (FurnChest)bagPrefab.Instance();

        tempBag.Name = "Created_" + tempBag.Name;
        tempBag.itemCodes = itemCodes;
        tempBag.ammoCount = ammoCount;

        Node parent = GetNode("/root/Main/Scene");
        parent.AddChild(tempBag);
        tempBag.Translation = Translation;
        tempBag.Translate(Vector3.Up / 4f);
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        base.TakeDamage(damager, damage, shapeID);
        coverTimer -= damage / 10;
    }
   
    protected override void AnimateDealth(Character killer, int shapeID)
    {
        if (itemCodes.Count > 0 || ammoCount.Count > 0) {
            SpawnItemsBag();
        }

        weapons.SetWeapon(false);
        base.AnimateDealth(killer, shapeID);
    }

    private void UpdatePath(float delta)
    {
        if (updatePath) {
            if (updatePathTimer > 0) {
                updatePathTimer -= delta;
            } else {
                path = null;
                updatePathTimer = 1;
                updatePath = false;
            }
        }
    }

    private void UpdateShooting(float victimDistance, float delta)
    {
        if (shootCooldown > 0) {
            shootCooldown -= delta;
        } else {
            shootCooldown = weapons.MakeShoot(victimDistance);
            coverTimer *= 0.75f;
        }
    }

    private void AttackEnemy(float delta)
    {
        if (weaponCode == "") {
            //если нет оружия
            //бегаем по укрытиям и молимся Селестии
            coverTimer = 0;
            return;
        }
        Vector3 victimPos = tempVictim.GlobalTransform.origin;
        float shootDistance = weapons.GetStatsFloat("shootDistance");
        float tempDistance = GlobalTransform.origin.DistanceTo(victimPos);
        if (cameToPlace && tempDistance < shootDistance) {
            UpdateShooting(tempDistance, delta);
            LookAtTarget();
            Stop();
        } else {
            GoTo(victimPos, shootDistance / 1.5f);
            updatePath = tempVictim.Velocity.Length() > 2;
        }
    }

    public void _on_lookArea_body_entered(Node body)
    {
        this.body._on_lookArea_body_entered(body);
    }

    public void _on_lookArea_body_exited(Node body)
    {
        this.body._on_lookArea_body_exited(body);
    }

    public override void _Ready()
    {
        base._Ready();
        rand.Randomize();
        body = new NPCBody(this);
        head = GetNode<NPCFace>("Armature/Skeleton/Body");
        weapons = GetNode<NPCWeapons>("weapons");
        navigation = GetNode<Navigation>("/root/Main/Scene/Navigation");
        covers = GetNode<CoversManager>("/root/Main/Scene/terrain/covers");
        bagPrefab = GD.Load<PackedScene>("res://objects/props/furniture/bag.tscn");
        if (IsUnicorn) {
            MagicParticles = GetNode<Particles>("Armature/Skeleton/BoneAttachment/HeadPos/Particles");
        }
        if (weaponCode != "") {
            weapons.LoadWeapon(this, weaponCode);
        } 
    }

    public override void _Process(float delta)
    {
        if (Health <= 0) {
            return;
        }

        base._Process(delta);
        body.Update(delta);
        UpdatePath(delta);

        switch (state) {
            case NPCState.Idle:
                if (patrolPoints == null || patrolPoints.Length == 0) {
                    if (!cameToPlace) {
                        GoTo(myStartPos, COME_DISTANCE, false);
                    } else {
                        GlobalTransform = Global.setNewOrigin(GlobalTransform, myStartPos);
                        Rotation = myStartRot;
                        body.PlayAnim(IdleAnim);
                    }
                } else {
                    if (patrolWaitTimer > 0) {
                        patrolWaitTimer -= delta;
                    } else {
                        GoTo(patrolPoints[patrolI].GlobalTransform.origin, COME_DISTANCE, false);
                        if (cameToPlace) {
                            if (patrolI < patrolPoints.Length - 1) {
                                patrolI += 1;
                            } else {
                                patrolI = 0;
                            }
                            patrolWaitTimer = PATROL_WAIT;
                        }
                    }
                }

                break;
            case NPCState.Attack:

                if (tempVictim.Health <= 0) {
                    SetState(NPCState.Idle);
                    return;
                }
                if (coverTimer > 0) {
                    coverTimer -= delta;
                    if (tempCover == null) {
                        //идем в атаку
                        AttackEnemy(delta);
                    } else {
                        //прячемся в укрытии
                        if (!cameToPlace) {
                            GoTo(tempCoverPlace, COME_DISTANCE);
                        } else {
                            LookAtTarget();
                            InCover = true;
                        }
                    }
                } else {
                    if (tempCover == null) {
                        FindCover();
                    } else {
                        StopHidingInCover();
                    }
                }
                
                break;
            case NPCState.Search:
                if (cameToPlace) {
                    if (searchTimer > 0) {
                        searchTimer -= delta;
                    } else {
                        SetState(NPCState.Idle);
                    }
                } else {
                    GoTo(lastSeePos);
                }
                break;
            
            case NPCState.Talk:
                Stop();
                LookAtTarget(false);
                break;
        }
    }
}
