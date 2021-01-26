using Godot;

public class BuckPostEffect: Effect 
{
    const int RECOIL_DELTA = 4;
    const int LEGS_DELTA = -20;
    const int HEALTH_DELTA = -30;
    private Player player;

    public BuckPostEffect()
    {
        maxTime = 10;
        badEffect = true;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "buck-after";
        if (!handler.HasEffect(this)) { 
            handler.SetPlayerParameter("recoil", ref player.BaseRecoil, RECOIL_DELTA);
            handler.SetPlayerParameter("legsDamage", ref player.LegsDamage, LEGS_DELTA);
            handler.SetPlayerParameter("healthMax", ref player.HealthMax, HEALTH_DELTA);
        }
        base.SetOn(handler);
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