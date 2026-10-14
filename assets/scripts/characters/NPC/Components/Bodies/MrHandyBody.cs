using Godot;

public partial class MrHandyBody : Node
{
    [Export] private AudioStreamWav hittedSound;
    [Export] private AudioStreamWav dieSound;
    
    private NpcAudio audi;
    private Node3D fire;
    
    public override void _Ready()
    {
        var npc = GetParent<NPC>();
        fire = npc.GetNode<Node3D>("Armature/Skeleton/BoneAttachment1/fire");
        audi = npc.GetNode<NpcAudio>("audi");
        
        var anim = npc.GetNode<AnimationPlayer>("anim");
        anim.Play("Idle");
        
        npc.Connect(Character.SignalName.TakenDamage, new Callable(this, nameof(OnNpcHitted)));
        npc.Connect(Character.SignalName.DieEvent, new Callable(this, nameof(OnNpcDying)));
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
