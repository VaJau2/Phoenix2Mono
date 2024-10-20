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
    [Export] public string npcCustomDialogueName = "";
    [Export] public float lookHeightFactor = -1.5f;
    [Export] public bool ignoreDamager;
    [Export] public Array<string> itemCodes = [];
    [Export] public Dictionary<string, int> ammoCount = new();
    [Export] public string customHintCode;
    [Export] public NodePath customInteractionTriggerPath;
    
    public NpcWeapons Weapons { get; private set; }
    public NpcCovers Covers { get; private set; }
    public SeekArea SeekArea { get; private set; }
    public BaseMovingController MovingController { get; private set; }
    public ChestHandler ChestHandler { get; private set; }
    public CachedHeadPosition HeadPosition;
    
    private NpcInteraction interaction;
    private StateMachine stateMachine;
    private NpcSkeleton skeleton;
    private NpcSaving saving;
    
    public bool MayInteract => interaction?.GetMayInteract() ?? false;
    public string InteractionHintCode => interaction?.GetInteractionHintCode();
    
    public string ChestCode => "body";
    
    public Dictionary<string, bool> objectsChangeActive = new();
    public Vector3 myStartPos, myStartRot;
    
    public bool aggressiveAgainstPlayer;
    public Character tempVictim;
    public Character followTarget;
    
    public bool MayChangeState = true;
    
    private static Player Player => Global.Get().player;
    
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
        Weapons = GetNodeOrNull<NpcWeapons>("weapons");
        Covers = GetNodeOrNull<NpcCovers>("covers");
        MovingController = GetNodeOrNull<BaseMovingController>("movingController");
        BaseSpeed = MovingController.BaseSpeed;
        stateMachine = GetNode<StateMachine>("stateMachine");
        interaction = GetNodeOrNull<NpcInteraction>("interaction");
        skeleton = GetNodeOrNull<NpcSkeleton>("Armature/Skeleton");
        
        if (myStartPos != Vector3.Zero || myStartRot != Vector3.Zero) return;
            
        myStartPos = GlobalTransform.origin;
        myStartRot = Rotation;
    }

    public void Interact(PlayerCamera interactor) => interaction?.Interact();

    public SetStateEnum GetState() => stateMachine.GetCurrentSetState();

    public void SetState(SetStateEnum state)
    {
        if (!MayChangeState) return;
        if (GetState() == state) return;
        stateMachine.SetState(state);
    }

    public Vector3 GetHeadPosition() => HeadPosition?.GetPosition() ?? GlobalTranslation;

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
            if (tempVictim == Player) 
            {
                Player.Stealth.RemoveSeekEnemy(this);
            }

            MakeDead();
            AnimateDeath(damager, shapeID);
        }
    }
    
    private void SetStateAgainstDamager(Character damager)
    {
        if (damager == Player && !aggressiveAgainstPlayer && GetState() == SetStateEnum.Idle)
        {
            aggressiveAgainstPlayer = true;
            SeekArea.AddEnemyInArea(Player);
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
        skeleton.CheckShotgunShot(isShotgun);
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
        myStartPos = newPos;
        
        if (MovingController is NavigationMovingController navigation)
        {
            navigation.RunToPoint = run;
            navigation.cameToPlace = false;
        }
       
        CleanPatrolArray();
    }

    public void CleanPatrolArray()
    {
        patrolArray?.Clear();
    }

    public void SetFollowTarget(Character newTarget)
    {
        followTarget = newTarget;
        
        if (MovingController is NavigationMovingController navigation)
        {
            navigation.cameToPlace = false;
        }
    }

    public void MakeDead(bool dropWeapon = true)
    {
        EmitSignal(nameof(IsDying));
        
        if (Weapons != null)
        {
            if (dropWeapon)
            {
                Weapons.SpawnPickableItem();
            }
            
            Weapons.SetWeaponOn(false);
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

        if (skeleton == null) return;
        
        skeleton.MakeDead();
            
        foreach (Node node in GetChildren())
        {
            if (node.Name == "Armature") continue;
            node.QueueFree();
        }
    }

    protected virtual void AnimateDeath(Character killer, int shapeID)
    {
        skeleton?.AnimateDeath(killer, shapeID);
    }
    
    public void _on_stopArea_body_entered(Node body)
    {
        if (MovingController is not NavigationMovingController navigation) return;
        switch (body)
        {
            case global::Player:
                navigation.stopAreaEntered = true;
                break;
            case FurnDoor { IsOpen: false } door:
                navigation?.SetDoorWait(door.ClickFurn());
                break;
        }
    }
    
    public void _on_stopArea_body_exited(Node body)
    {
        if (body is Player && MovingController is NavigationMovingController navigation)
        {
            navigation.stopAreaEntered = false;
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
    Neitral,
    Monster // ридер врет, этот релейшен используется
}
