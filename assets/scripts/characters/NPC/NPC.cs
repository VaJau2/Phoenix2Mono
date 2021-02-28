using Godot;
using Godot.Collections;

//класс отвечает за поведение НПЦ
public class NPC : Character
{
    const int RAGDOLL_IMPULSE = 700;
    const float SEARCH_TIMER = 5f;
    private float ROTATION_SPEED = 0.45f;
    protected float GRAVITY = 6;
    protected float PATROL_WAIT = 4f;
    [Export]
    public Array<NodePath> patrolArray;
    protected Spatial[] patrolPoints;
    protected int patrolI = 0;
    protected float patrolWaitTimer = 0;
    [Export]
    public string IdleAnim = "";

    [Export]
    public int StartHealth = 100;
    [Export]
    public Relation relation;
    [Export]
    public string dialogueCode = "";
    [Export]
    public int WalkSpeed = 5;
    public bool aggressiveAgainstPlayer;
    public NPCState state;
    public NPCFace head;
    protected SeekArea seekArea;
    protected AudioStreamPlayer3D audi;
    private Skeleton skeleton;
    private PhysicalBone headBone;
    private PhysicalBone bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    private Player player => Global.Get().player;
    public Character tempVictim;
    protected Vector3 lastSeePos;
    protected float searchTimer = 0;

    protected bool CloseToPoint = false;

    protected Vector3 myStartPos, myStartRot;


    public virtual void SetState(NPCState newState)
    {
        if (Health <= 0) {
            return;
        }

        switch(newState) {
            case NPCState.Idle:
                if (tempVictim == player) {
                    player.Stealth.RemoveSeekEnemy(this);
                }
                tempVictim = null;
                break;
            
            case NPCState.Attack:
                if (tempVictim == player) {
                    player.Stealth.AddAttackEnemy(this);
                }
                break;
            
            case NPCState.Search:
                if (tempVictim == player) {
                    player.Stealth.AddSeekEnemy(this);
                }

                lastSeePos = tempVictim.GlobalTransform.origin;
                searchTimer = SEARCH_TIMER;
                break;
        }
        state = newState;
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        if (state != NPCState.Attack) {
            tempVictim = damager;
            SetState(NPCState.Attack);
        }
        if (damager == player && !aggressiveAgainstPlayer) {
            aggressiveAgainstPlayer = true;
            seekArea.AddEnemyInArea(player);
        }

        if (shapeID != 0) {
            damage = (int)(damage * 1.5f);
        }

        base.TakeDamage(damager, damage, shapeID);

        if (Health <= 0) {
            if (tempVictim == player) {
                player.Stealth.RemoveAttackEnemy(this);
                player.Stealth.RemoveSeekEnemy(this);
            }

            AnimateDealth(damager, shapeID);
        }
    }

    public override void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
    }

    protected virtual async void AnimateDealth(Character killer, int shapeID)
    {
        CollisionLayer = 0;
        CollisionMask = 0;
        skeleton.PhysicalBonesStartSimulation();
        Vector3 dir = Translation.DirectionTo(killer.Translation);
        float force = tempShotgunShot ? RAGDOLL_IMPULSE * 2 : RAGDOLL_IMPULSE;

        if (shapeID == 0) {
            bodyBone.ApplyCentralImpulse(-dir * force);
        } else {
            headBone.ApplyCentralImpulse(-dir * force);
        }
        
        await Global.Get().ToTimer(5f);
        QueueFree();
    }

    protected void MoveTo(Vector3 place, float distance, float speed = 1)
    {
        var pos = GlobalTransform.origin;
        place.y = pos.y;

        var rotA = Transform.basis.Quat().Normalized();
        var rotB = Transform.LookingAt(place, Vector3.Up).basis.Quat().Normalized();
        var tempRotation = rotA.Slerp(rotB, ROTATION_SPEED);

        Transform tempTransform = Transform;
        tempTransform.basis = new Basis(tempRotation);
        Transform = tempTransform;

        speed += BaseSpeed;

        Rotation = new Vector3(0, Rotation.y, 0);
        Velocity = new Vector3(0, -GRAVITY, -speed).Rotated(Vector3.Up, Rotation.y);

        var temp_distance = pos.DistanceTo(place);
        CloseToPoint = temp_distance <= distance;
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        skeleton = GetNode<Skeleton>("Armature/Skeleton");
        head = GetNode<NPCFace>("Armature/Skeleton/Body");
        seekArea = GetNode<SeekArea>("seekArea");
        
        headBone = GetNode<PhysicalBone>("Armature/Skeleton/Physical Bone neck");
        bodyBone = GetNode<PhysicalBone>("Armature/Skeleton/Physical Bone back_2");

        SetStartHealth(StartHealth);
        BaseSpeed = WalkSpeed;

        if (patrolArray == null || patrolArray.Count == 0) {
            myStartPos = GlobalTransform.origin;
            myStartRot = Rotation;
        } else {
            patrolPoints = new Spatial[patrolArray.Count];
            for(int i = 0; i < patrolArray.Count; i++) {
                patrolPoints[i] = GetNode<Spatial>(patrolArray[i]);
            }
        }
    }

    public override void _Process(float delta)
    {
        if (Health <= 0) {
            return;
        }

        if (state == NPCState.Search) {
            if (searchTimer > 0) {
                searchTimer -= delta;
            } else {
                SetState(NPCState.Idle);
            }
        }

        if (Velocity.Length() > 0) {
            MoveAndSlide(Velocity);
        }
    }
}

public enum Relation {
    Friend,
    Enemy
}

public enum NPCState {
    Idle,
    Attack,
    Search,
    Talk
}