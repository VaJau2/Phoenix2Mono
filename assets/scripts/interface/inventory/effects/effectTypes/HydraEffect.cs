using Godot;

public partial class HydraEffect: Effect 
{
    const float HEAL_COOLDOWN = 2f;
    private Player player;
    private int healCount = 2;
    private float cooldown = 0;

    public HydraEffect()
    {
        maxTime = 50;
        badEffect = true;
        postEffectChance = 0.5f;
        postEffect = new HydraPostEffect();
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "hydra";
        base.SetOn(handler);
    }

    public override bool Count(float delta)
    {
        if (cooldown > 0) {
            cooldown -= delta;
        } else {
            player.HealHealth(healCount);
            cooldown = HEAL_COOLDOWN;
        }
        
        return base.Count(delta);
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff(startPostEffect);

        if (!handler.HasEffect(this)) {
            if (startPostEffect) {
                StartPostEffect();
            }
        }
    }
}