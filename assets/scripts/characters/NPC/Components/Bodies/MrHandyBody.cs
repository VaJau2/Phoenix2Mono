using Godot;

public partial class MrHandyBody : Node
{
    [Export] private AudioStreamWAV hittedSound;
    [Export] private AudioStreamWAV dieSound;
    
    private NpcAudio audi;
    private Node3D fire;
    
    public override void _Ready()
    {
        var npc = GetParent<NPC>();
        fire = npc.GetNode<Node3D>("Armature/Skeleton3D/BoneAttachment1/fire");
        audi = npc.GetNode<NpcAudio>("audi");
        
        var anim = npc.GetNode<AnimationPlayer>("anim");
        anim.Play("Idle");
        
        npc.Connect(nameof(Character.TakenDamage), new Callable(this, nameof(OnNpcHitted)));
        npc.Connect(nameof(Character.DieEvent), new Callable(this, nameof(OnNpcDying)));
    }
    
    public void OnNpcHitted()
    {
        audi.PlayStream(hittedSound);
    }
    
    public void OnNpcDying()
    {
        audi.PlayStream(dieSound);
        fire.QueueFree();
    }
}
