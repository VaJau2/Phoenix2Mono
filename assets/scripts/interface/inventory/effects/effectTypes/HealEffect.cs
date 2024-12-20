namespace Effects;

public class HealEffect : Effect
{
    const float HEAL_COOLDOWN = 0.6f;
    private Player player;
    private int healCount = 2;
    private float cooldown = 0;

    public HealEffect()
    {
        maxTime = 30;
        badEffect = false;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "healPotion";
        base.SetOn(handler);
    }

    public override bool Count(float delta)
    {
        if (cooldown > 0)
        {
            cooldown -= delta;
        }
        else
        {
            player.HealHealth(healCount);
            cooldown = HEAL_COOLDOWN;
        }

        return base.Count(delta);
    }
}