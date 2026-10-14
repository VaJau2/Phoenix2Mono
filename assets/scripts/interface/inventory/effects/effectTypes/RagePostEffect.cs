namespace Effects;

public class RagePostEffect : Effect
{
    const int DAMAGE_DELTA = -20;
    private Player player;

    public RagePostEffect()
    {
        maxTime = 110;
        emotion = "meds_after";
        badEffect = true;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "rage-after";
        base.SetOn(handler);

        if (!handler.HasEffect(this))
        {
            handler.SetPlayerParameter("damage", ref player.BaseDamage, DAMAGE_DELTA);
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff(startPostEffect);
        if (!handler.HasEffect(this))
        {
            handler.ClearPlayerParameter("damage", ref player.BaseDamage);
        }
    }
}