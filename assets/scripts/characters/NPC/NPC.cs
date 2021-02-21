using Godot;
using Godot.Collections;

//класс отвечает за поведение НПЦ
public class NPC : Character
{
    const int RAGDOLL_IMPULSE = 700;
    const float SEARCH_TIMER = 5f;

    [Export]
    public int StartHealth = 100;
    [Export]
    public string weaponCode;
    [Export]
    public Array<string> itemCodes = new Array<string>();
    [Export]
    public Dictionary<string, int> ammoCount = new Dictionary<string, int>();
    [Export]
    public Relation relation;
    public bool aggressiveAgainstPlayer;
    public NPCState state;

    public NPCFace head;
    private NPCWeapons weapons;
    public AnimationPlayer anim;
    private  AudioStreamPlayer3D audi;
    private Skeleton skeleton;
    private PhysicalBone headBone;
    private PhysicalBone bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    private PackedScene bagPrefab;

    private Player player => Global.Get().player;
    public Character tempVictim;
    private Vector3 lastSeePos;
    private float searchTimer = 0;

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

                weapons.SetWeapon(false);
                tempVictim = null;
                break;
            
            case NPCState.Attack:
                if (tempVictim == player) {
                    player.Stealth.AddAttackEnemy(this);
                }

                weapons.SetWeapon(true);
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
        GD.Print(this.Name + ": new state = " + newState);
    }

    public virtual Spatial GetWeaponParent(bool isPistol)
    {
        if (isPistol) {
            return GetNode<Spatial>("Armature/Skeleton/BoneAttachment/weapons");
        } else {
            return GetNode<Spatial>("Armature/Skeleton/BoneAttachment 2/weapons");
        }
    }

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        if (state != NPCState.Attack) {
            tempVictim = damager;
            SetState(NPCState.Attack);
        }
        if (damager == player) {
            aggressiveAgainstPlayer = true;
        }

        if (shapeID != 0) {
            damage = (int)(damage * 1.5f);
        }

        base.TakeDamage(damager, damage, shapeID);

        if (Health <= 0) {
            if (tempVictim == player) {
                GD.Print(this.Name + " remove seek and attack enemy");
                player.Stealth.RemoveAttackEnemy(this);
                player.Stealth.RemoveSeekEnemy(this);
            }

            if (itemCodes.Count > 0) {
                SpawnItemsBag();
            }

            AnimateDealth(damager, shapeID);
        }
    }

    public override void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
    }

    private void SpawnItemsBag()
    {
        FurnChest tempBag = (FurnChest)bagPrefab.Instance();

        tempBag.itemCodes = itemCodes;
        tempBag.ammoCount = ammoCount;

        Node parent = GetNode("/root/Main/Scene");
        parent.AddChild(tempBag);
        tempBag.Translation = Translation;
        tempBag.Translate(Vector3.Up / 4f);
    }

    private async void AnimateDealth(Character killer, int shapeID)
    {
        weapons.SetWeapon(false);
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

    public override void _Ready()
    {
        anim = GetNode<AnimationPlayer>("anim");
        audi = GetNode<AudioStreamPlayer3D>("audi");
        skeleton = GetNode<Skeleton>("Armature/Skeleton");
        head = GetNode<NPCFace>("Armature/Skeleton/Body");
        weapons = GetNode<NPCWeapons>("weapons");
        headBone = GetNode<PhysicalBone>("Armature/Skeleton/Physical Bone neck");
        bodyBone = GetNode<PhysicalBone>("Armature/Skeleton/Physical Bone back_2");
        bagPrefab = GD.Load<PackedScene>("res://objects/props/furniture/bag.tscn");

        anim.Play("Idle");
        SetStartHealth(StartHealth);

        if (weaponCode != "") {
            weapons.LoadWeapon(this, weaponCode);
        } 
    }

    public override void _Process(float delta)
    {
        switch(state) {

            case NPCState.Search:
                if (searchTimer > 0) {
                    searchTimer -= delta;
                } else {
                    SetState(NPCState.Idle);
                }
                break;

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
    Search
}