using Godot;
using Godot.Collections;

public class DragonBody: Node
{
    public const int MOUTH_DAMAGE = 40;
    public const int FIRE_DAMAGE = 30;
    public const int IDLE_FLY_HEIGHT = 40;
    
    private NPC npc;
    private NpcAudio audi;
    private DragonMouth mouth;
    private AnimationPlayer anim;
    
    [Export] private Array<AudioStreamSample> idleSounds;
    [Export] private AudioStreamSample fireSound;

    private Spatial fireObj;
    private Array<Particles> fireParts = new();
    private AnimationPlayer fireAnim;
    private AudioStreamPlayer3D audiFire;
    
    private float idleSoundTimer = 5f;
    private bool onetimeDie;
    private bool onetimeAnim;
    private bool isFiring;
    
    public override void _Ready()
    {
        npc = GetParent<NPC>();
        audi = npc.GetNode<NpcAudio>("audi");
        
        anim = GetNode<AnimationPlayer>("anim");
        fireObj = GetNode<Spatial>("Armature/Skeleton/BoneAttachment/mouth/fire");
        fireParts.Add(fireObj.GetNode<Particles>("Particles"));
        fireParts.Add(fireObj.GetNode<Particles>("Particles2"));
        fireParts.Add(fireObj.GetNode<Particles>("Particles3"));
        fireAnim = fireObj.GetNode<AnimationPlayer>("fireAnim");
        audiFire = GetNode<AudioStreamPlayer3D>("audi-fire");
        
        npc.Connect(nameof(NPC.TakeDamage), this, nameof(OnTakeDamage));
    }

    public override void _Process(float delta)
    {
        if (npc.Velocity.Length() > 0 && !onetimeAnim)
        {
            UpdateFlyAnims();
        }
    }

    public int GetDamage(int baseDamage)
    {
        float tempDamage = baseDamage;
        tempDamage *= Global.Get().Settings.npcDamage;
        return (int) tempDamage;
    }
    
    public float GetEnemyDistance()
    {
        if (IsInstanceValid(npc.tempVictim))
        {
            return npc.GlobalTranslation.DistanceTo(npc.tempVictim.GlobalTranslation);
        }

        return -1;
    }
    
    private void OnTakeDamage()
    {
        SetFireOn(false);
        GetNode<AudioStreamPlayer3D>("audi-wings").Stop();
    }
    
    public void PlayIdleSounds(float delta)
    {
        if (idleSoundTimer > 0)
        {
            idleSoundTimer -= delta;
        }
        else
        {
            audi.PlayRandomSound(idleSounds);
            var rand = new RandomNumberGenerator();
            idleSoundTimer = rand.RandfRange(2, 10);
        }
    }
    
    private async void UpdateFlyAnims()
    {
        if (mouth.HasEnemy)
        {
            if (anim.CurrentAnimation != "fly-open-mouth")
            {
                anim.Play("fly-open-mouth");
            }
            
            return;
        }
        
        onetimeAnim = true;
        var rotY1 = npc.Rotation.y;
        await Global.Get().ToTimer(0.1f, this);
        var rotY2 = npc.Rotation.y;
        if (rotY2 < rotY1)
        {
            if (anim.CurrentAnimation != "fly-right")
            {
                anim.Play("fly-right");
            }
        }
        else if (rotY2 > rotY1)
        {
            if (anim.CurrentAnimation != "fly-left")
            {
                anim.Play("fly-left");
            }
        }
        else
        {
            if (anim.CurrentAnimation != "fly")
            {
                anim.Play("fly");
            }
        }

        onetimeAnim = false;
    }
    
    public void SetFireOn(bool on)
    {
        if (isFiring == on) return;

        isFiring = on;
        anim.Play("attack");
        
        foreach (Particles fire in fireParts)
        {
            fire.Emitting = on;
        }

        if (on)
        {
            audiFire.Stream = fireSound;
            audiFire.Play();
            fireAnim.Play("fire");
        }
        else
        {
            audiFire.Stop();
            fireAnim.CurrentAnimation = null;
            fireObj.GetNode<Sprite3D>("sprite").Frame = 0;
            fireObj.GetNode<Sprite3D>("sprite2").Frame = 0;
            fireObj.GetNode<Sprite3D>("sprite3").Frame = 0;
        }
    }

    public void Stop()
    {
        npc.Velocity = Vector3.Zero;
        anim.Play("fly-on-place");
    }
}