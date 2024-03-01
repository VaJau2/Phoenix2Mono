using Godot;

public partial class MrHandy : NpcWithWeapons
{
    private AnimationPlayer anim;

    public override Node3D GetWeaponParent(bool isPistol)
    {
        return GetNode<Node3D>("Armature/Skeleton3D/mrHandy/weapons");
    }

    protected override void AnimateDeath(Character killer, int shapeID)
    {
        GetNode<Node3D>("Armature/Skeleton3D/BoneAttachment3D/fire").QueueFree();
        base.AnimateDeath(killer, shapeID);
    }


    public override void _Ready()
    {
        base._Ready();
        
        anim = GetNode<AnimationPlayer>("anim");
        anim.Play(IdleAnim);
    }

    public override void _Process(double delta)
    {
        if (Health <= 0)
        {
            return;
        }

        base._Process(delta);
        UpdatePath((float)delta);
        UpdateAI((float)delta);
    }
}
