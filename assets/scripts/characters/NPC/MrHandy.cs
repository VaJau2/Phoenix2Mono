using Godot;

public class MrHandy : NpcWithWeapons
{
    private AnimationPlayer anim;

    public override Spatial GetWeaponParent(bool isPistol)
    {
        return GetNode<Spatial>("Armature/Skeleton/mrHandy/weapons");
    }

    protected override void AnimateDeath(Character killer, int shapeID)
    {
        GetNode<Spatial>("Armature/Skeleton/BoneAttachment/fire").QueueFree();
        base.AnimateDeath(killer, shapeID);
    }


    public override void _Ready()
    {
        base._Ready();
        
        anim = GetNode<AnimationPlayer>("anim");
        anim.Play(IdleAnim);
    }

    public override void _Process(float delta)
    {
        if (Health <= 0)
        {
            return;
        }

        base._Process(delta);
        UpdatePath(delta);
        UpdateAI(delta);
    }
}
