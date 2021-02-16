using Godot;
using Godot.Collections;

//класс отвечает за поведение НПЦ
public class NPC : Character
{
    const int RAGDOLL_IMPULSE = 700;

    [Export]
    public Array<string> itemCodes = new Array<string>();
    [Export]
    public Dictionary<string, int> ammoCount = new Dictionary<string, int>();
    [Export]
    public bool SpawnRandomItems;

    [Export]
    public int StartHealth = 100;

    private NPCFace head;
    private AnimationPlayer anim;
    private  AudioStreamPlayer3D audi;
    private Skeleton skeleton;
    private PhysicalBone headBone;
    private PhysicalBone bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    private PackedScene bagPrefab;

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        if (shapeID != 0) {
            damage = (int)(damage * 1.5f);
        }

        base.TakeDamage(damager, damage, shapeID);
        if (Health <= 0) {
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
        headBone = GetNode<PhysicalBone>("Armature/Skeleton/Physical Bone neck");
        bodyBone = GetNode<PhysicalBone>("Armature/Skeleton/Physical Bone back_2");
        bagPrefab = GD.Load<PackedScene>("res://objects/props/furniture/bag.tscn");

        anim.Play("Idle");
        SetStartHealth(StartHealth);

        if (SpawnRandomItems) {
            RandomItems items = GetNode<RandomItems>("/root/Main/Scene/randomItems");
            items.LoadRandomItems(itemCodes, ammoCount);
        }
    }

    public override void _Process(float delta)
    {
    }
}
