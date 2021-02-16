using Godot;

//класс отвечает за поведение НПЦ
public class NPC : Character
{
    const int RAGDOLL_IMPULSE = 700;
    NPCFace head;
    AnimationPlayer anim;
    AudioStreamPlayer3D audi;
    Skeleton skeleton;
    PhysicalBone headBone;
    PhysicalBone bodyBone;
    bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика

    public override void TakeDamage(Character damager, int damage, int shapeID = 0)
    {
        if (shapeID != 0) {
            damage = (int)(damage * 1.5f);
        }

        base.TakeDamage(damager, damage, shapeID);
        if (Health <= 0) {
            AnimateDealth(damager, shapeID);
        }
    }

    public override void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
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

        anim.Play("Idle");
        SetStartHealth(100);
    }

    public override void _Process(float delta)
    {
    }
}
