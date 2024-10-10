using Godot;

public class RoboEyeBody : Node
{
    [Export] private AudioStreamSample dieSound;
    [Export] private AudioStreamSample walkSound;

    public NPC npc;
    private NpcAudio audi;
    private AnimationPlayer anim;
    private RoboEyeMaterial tempMaterial;
    
    public override void _Ready()
    {
        npc = GetParent<NPC>();
        audi = npc.GetNode<NpcAudio>("audi");
        anim = npc.GetNode<AnimationPlayer>("anim");
        anim.Play("Idle");
        
        npc.Connect(nameof(NPC.IsDying), this, nameof(OnNpcDying));
    }

    public override void _Process(float delta)
    {
        if (npc.Velocity.Length() > Character.MIN_WALKING_SPEED)
        {
            anim.Play("walk");
            
            if (!audi.Playing)
            {
                audi.PlayStream(walkSound);
            }
        }
    }
    
    public async void Resurrect()
    {
        var defaultSpeed = npc.BaseSpeed;
        npc.BaseSpeed = 0;
        npc.CollisionLayer = 1;
        npc.CollisionMask = 1;
        
        anim.PlayBackwards("Die");

        await ToSignal(anim, "animation_finished");

        npc.BaseSpeed = defaultSpeed;
        npc.SetStartHealth(npc.HealthMax);
    }

    public void Disable()
    {
        anim.CurrentAnimation = null;
    }
    
    public void ChangeMaterial(RoboEyeMaterial material = RoboEyeMaterial.Default)
    {
        if (tempMaterial == material) return;

        var materialName = material != RoboEyeMaterial.Default 
            ? material.ToString().ToLower()
            : "";
        
        string path = "res://assets/models/characters/robots/roboEye/roboEye";
        path += (materialName != "") ? "-" + materialName : "";
        path += ".material";
        SpatialMaterial newMaterial = GD.Load<SpatialMaterial>(path);

        var monitor = GetNode("../corpus/monitor");
        monitor.GetNode<MeshInstance>("screen").SetSurfaceMaterial(0, newMaterial);
        monitor.GetNode<MeshInstance>("lamp").SetSurfaceMaterial(0, newMaterial);
        tempMaterial = material;
    }
    
    private void OnNpcDying()
    {
        audi.PlayStream(dieSound);
        
        anim.Play("Die");
    }

}

public enum RoboEyeMaterial
{
    Red,
    Orange,
    Default,
    Dead
}
