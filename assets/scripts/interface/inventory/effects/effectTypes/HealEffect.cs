using Godot;

public class HealEffect: Effect 
{
    const float HEAL_COOLDOWN = 2f;
    private Player player;
    private int healCount = 2;
    private float cooldown = 0;

    public HealEffect() 
    {
        maxTime = 60;
        badEffect = false;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "heal-potion";
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
}