using Godot;

public class MrHandyBody : Node
{
    [Export] private AudioStreamSample hittedSound;
    [Export] private AudioStreamSample dieSound;
    
    private NpcAudio audi;
    private Spatial fire;
    
    public override void _Ready()
    {
        var npc = GetParent<NPC>();
        fire = npc.GetNode<Spatial>("Armature/Skeleton/BoneAttachment/fire");
        audi = npc.GetNode<NpcAudio>("audi");
        
        var anim = GetNode<AnimationPlayer>("anim");
        anim.Play("Idle");
        
        npc.Connect(nameof(NPC.IsDying), this, nameof(OnNpcDying));
        npc.Connect(nameof(Character.TakenDamage), this, nameof(OnNpcHitted));
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
