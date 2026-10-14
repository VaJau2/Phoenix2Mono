using Godot;

public partial class RoboEyeBody : Node
{
    [Export] private AudioStreamWav dieSound;
    [Export] private AudioStreamWav walkSound;

    public string IdleAnim = "idle";

    private NPC npc;
    private NpcAudio audi;
    private AnimationPlayer anim;
    private RoboEyeMaterial tempMaterial;
    
    public override void _Ready()
    {
        npc = GetParent<NPC>();
        audi = npc.GetNode<NpcAudio>("audi");
        anim = npc.GetNode<AnimationPlayer>("anim");
        
        npc.Connect(Character.SignalName.DieEvent, new Callable(this, nameof(OnNpcDying)));
    }

    public override void _Process(double delta)
    {
        if (npc.Health <= 0) return;
        if (tempMaterial == RoboEyeMaterial.Dead) return;
        
        if (npc.Velocity.Length() > Character.MIN_WALKING_SPEED)
        {
            anim.Play("walk");
            
            if (!audi.Playing)
            {
                audi.PlayStream(walkSound);
            }
        }
        else if (!string.IsNullOrEmpty(IdleAnim) && anim.CurrentAnimation == "walk")
        {
            anim.Play(IdleAnim);
            audi.Stop();
        }
    }
    
    public async void Resurrect()
    {
        var defaultSpeed = npc.BaseSpeed;
        npc.BaseSpeed = 0;
        npc.CollisionLayer = 1;
        npc.CollisionMask = 1;
        npc.SetStartHealth(npc.HealthMax);
        
        ChangeMaterial();
        anim.PlayBackwards("Die");

        await ToSignal(anim, "animation_finished");

        npc.BaseSpeed = defaultSpeed;
    }

    public void Disable()
    {
        anim.Stop();
    }
    
    public void ChangeMaterial(RoboEyeMaterial material = RoboEyeMaterial.Default)
    {
        if (tempMaterial == material) return;

        var materialName = material != RoboEyeMaterial.Default 
            ? material.ToString().ToLower()
            : "";
        
        string path = "res://assets/models/characters/robots/roboEye/roboEye";
        path += (materialName != "") ? "-" + materialName : "";
        path += ".tres";
        StandardMaterial3D newMaterial = GD.Load<StandardMaterial3D>(path);

        var monitor = GetNode("../corpus/monitor");
        monitor.GetNode<MeshInstance3D>("screen").SetSurfaceOverrideMaterial(0, newMaterial);
        monitor.GetNode<MeshInstance3D>("lamp").SetSurfaceOverrideMaterial(0, newMaterial);
        tempMaterial = material;
    }
    
    private void OnNpcDying()
    {
        ChangeMaterial(RoboEyeMaterial.Dead);
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
