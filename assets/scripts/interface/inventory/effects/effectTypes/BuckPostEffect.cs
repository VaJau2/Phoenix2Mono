namespace Effects;

public class BuckPostEffect: Effect 
{
    const int RECOIL_DELTA = 4;
    const int LEGS_DELTA = -20;
    const int HEALTH_DELTA = -30;
    private Player player;

    public BuckPostEffect()
    {
        maxTime = 120;
        badEffect = true;
        emotion = "meds_after";
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "buck-after";
        base.SetOn(handler);
        
        if (!handler.HasEffect(this)) { 
            handler.SetPlayerParameter("recoil", ref player.BaseRecoil, RECOIL_DELTA);
            handler.SetPlayerParameter("legsDamage", ref player.LegsDamage, LEGS_DELTA);
            handler.SetPlayerParameter("healthMax", ref player.HealthMax, HEALTH_DELTA);
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff();
        if (!handler.HasEffect(this)) {
            handler.ClearPlayerParameter("recoil", ref player.BaseRecoil);
            handler.ClearPlayerParameter("legsDamage", ref player.LegsDamage);
            handler.ClearPlayerParameter("healthMax", ref player.HealthMax);
        }
    }
}