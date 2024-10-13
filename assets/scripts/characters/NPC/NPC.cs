using Godot;
using Godot.Collections;

//класс отвечает за поведение НПЦ
public class NPC : Character, IInteractable, IChest
{
    [Export] public Array<NodePath> patrolArray = [];

    [Export] public bool IsImmortal;
    [Export] public int StartHealth = 100;
    [Export] public Relation relation;
    [Export] public string subtitlesCode = "";
    [Export] public string dialogueCode = "";

    [Export] public float lookHeightFactor = -1.5f;
    public bool aggressiveAgainstPlayer;
    [Export] public bool ignoreDamager;
    
    [Export] public Array<string> itemCodes = [];
    [Export] public Dictionary<string, int> ammoCount = new();

    [Export] public string customHintCode;
    [Export] public NodePath customInteractionTriggerPath;
    
    public string ChestCode => "body";
    
    public Vector3 myStartPos, myStartRot;

    public virtual bool MayInteract => interaction?.GetMayInteract() ?? false;
    public virtual string InteractionHintCode => interaction?.GetInteractionHintCode();
    
    public NPCWeapons Weapons { get; private set; }
    public NPCCovers Covers { get; private set; }
    public SeekArea SeekArea { get; private set; }
    public NavigationMovingController MovingController { get; private set; }
    public ChestHandler ChestHandler { get; private set; }

    private NpcInteraction interaction;
    private StateMachine stateMachine;
    private NpcSaving saving;
    
    [Export] public bool hasSkeleton = true;
    public Skeleton skeleton;
    [Export] private NodePath headBonePath, bodyBonePath;
    
    public Dictionary<string, bool> objectsChangeActive = new();
    private CachedHeadPosition headPosition;
    private PhysicalBone headBone;
    private PhysicalBone bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    public Character tempVictim;
    public Character followTarget;
    
    private Player player => Global.Get().player;
    
    [Signal]
    public delegate void IsCame();
    
    [Signal]
    public delegate void FoundEnemy();
    
    [Signal]
    public delegate void IsDying();
    
    public override void _Ready()
    {
        SetStartHealth(StartHealth);
        
        ChestHandler = new ChestHandler(this)
            .SetCode(ChestCode)
            .LoadStartItems(itemCodes, ammoCount);

        saving = new NpcSaving(this);
        SeekArea = GetNode<SeekArea>("seekArea");
        Weapons = GetNodeOrNull<NPCWeapons>("weapons");
        Covers = GetNodeOrNull<NPCCovers>("covers");
        MovingController = GetNodeOrNull<NavigationMovingController>("movingController");
        BaseSpeed = MovingController.WalkSpeed;
        stateMachine = GetNode<StateMachine>("stateMachine");
        interaction = GetNodeOrNull<NpcInteraction>("interaction");
        
        if (hasSkeleton)
        {
            skeleton = GetNode<Skeleton>("Armature/Skeleton");

            headPosition = skeleton.GetNode<CachedHeadPosition>("BoneAttachment");
            
            headBone = headBonePath != null ? GetNodeOrNull<PhysicalBone>(headBonePath) 
                : skeleton.GetNodeOrNull<PhysicalBone>("Physical Bone neck");
            bodyBone = bodyBonePath != null ? GetNodeOrNull<PhysicalBone>(bodyBonePath) 
                : skeleton.GetNodeOrNull<PhysicalBone>("Physical Bone back_2");
        }
        
        if (myStartPos != Vector3.Zero || myStartRot != Vector3.Zero) return;
            
        myStartPos = GlobalTransform.origin;
        myStartRot = Rotation;
    }

    public void Interact(PlayerCamera interactor) => interaction?.Interact();

    public SetStateEnum GetState() => stateMachine.GetCurrentSetState();

    public void SetState(SetStateEnum state)
    {
        stateMachine.SetState(state);
    }

    public Vector3 GetHeadPosition() => headPosition?.GetPosition() ?? GlobalTranslation;

    public override int GetDamage()
    {
        float tempDamage = base.GetDamage();
        tempDamage *= Global.Get().Settings.npcDamage;
        return (int) tempDamage;
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        EmitSignal(nameof(TakenDamage));
        
        if (IsImmortal) return;
        
        if (shapeID != 0) 
        {
            damage = (int)(damage * 1.5f);
        }

        base.TakeDamage(damager, damage, shapeID);

        if (Health > 0) 
        {
            if (!ignoreDamager)
            {
                SetStateAgainstDamager(damager);
            }
        }
        else
        {
            if (tempVictim == player) 
            {
                player.Stealth.RemoveSeekEnemy(this);
            }

            MakeDead();
            AnimateDeath(damager, shapeID);
        }
    }
    
    private void SetStateAgainstDamager(Character damager)
    {
        if (damager == player && !aggressiveAgainstPlayer && GetState() == SetStateEnum.Idle)
        {
            aggressiveAgainstPlayer = true;
            SeekArea.AddEnemyInArea(player);
        }
        
        var coverChance = 1.0f - (float)Health / HealthMax;

        var rand = new RandomNumberGenerator();
        var newState = rand.Randf() < coverChance ? SetStateEnum.Hiding : SetStateEnum.Attack;
        if (GetState() == newState) return;
        
        tempVictim = damager;
        SetState(newState);
    }

    public override void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
    }

    public void SetObjectActive(string objectPath, bool active)
    {
        var showObject = GetNode<Spatial>(objectPath);
        if (showObject == null) return;
        showObject.Visible = active;
        objectsChangeActive[objectPath] = active;
    }

    public void SetNewStartPos(Vector3 newPos, bool run = false)
    {
        MovingController.RunToPoint = run;
        myStartPos = newPos;
        MovingController.cameToPlace = false;
        CleanPatrolArray();
    }

    public void CleanPatrolArray()
    {
        patrolArray?.Clear();
    }

    public void SetFollowTarget(Character newTarget)
    {
        followTarget = newTarget;
    }

    public void MakeDead()
    {
        EmitSignal(nameof(IsDying));
        
        if (Weapons != null)
        {
            Weapons.SetWeaponOn(false);
            Weapons.SpawnPickableItem();
            Weapons.WeaponCode = null;
        }
        
        if (GetParent() is EnemiesManager manager)
        {
            if (manager.enemies.Contains(this))
            {
                manager.enemies.Remove(this);
            }
        }
        
        CollisionLayer = 2;
        CollisionMask = 2;

        if (!hasSkeleton) return;
        
        skeleton.PhysicalBonesStartSimulation();
            
        foreach (Node node in GetChildren())
        {
            if (node.Name == "Armature") continue;
            node.QueueFree();
        }
            
        foreach (var boneObject in skeleton.GetChildren())
        {
            if (boneObject is not PhysicalBone bone) continue;
            bone.CollisionLayer = 6;
            bone.CollisionMask = 6;
        }
    }

    protected virtual void AnimateDeath(Character killer, int shapeID)
    {
        if (hasSkeleton)
        {
            Vector3 dir = Translation.DirectionTo(killer.Translation);
            float force = tempShotgunShot ? MovingController.RagdollImpulse * 1.5f : MovingController.RagdollImpulse;

            if (shapeID == 0)
            {
                bodyBone?.ApplyCentralImpulse(-dir * force);
            }
            else
            {
                headBone?.ApplyCentralImpulse(-dir * force);
            }
        }
    }
    
    public void _on_stopArea_body_entered(Node body)
    {
        switch (body)
        {
            case Player when MovingController != null:
                MovingController.stopAreaEntered = true;
                break;
            case FurnDoor { IsOpen: false } door:
                MovingController?.SetDoorWait(door.ClickFurn());
                break;
        }
    }
    
    public void _on_stopArea_body_exited(Node body)
    {
        if (body is Player && MovingController != null)
        {
            MovingController.stopAreaEntered = false;
        }
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        saving.LoadData(data);
    }

    public override Dictionary GetSaveData()
    {
        var saveData = DictionaryHelper.Merge( 
        base.GetSaveData(), 
        saving.GetSaveData()
        );
        
        return saveData;
    }
}

public enum Relation {
    Friend,
    Enemy,
    Neitral
}
