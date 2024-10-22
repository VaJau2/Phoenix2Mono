namespace Effects;

public class BuckEffect : Effect 
{
    const int RECOIL_DELTA = -2;
    const int LEGS_DELTA = 20;
    const int HEALTH_DELTA = 30;
    private Player player;
    
    public BuckEffect()
    {
        maxTime = 60;
        badEffect = true;
        postEffectChance = 0.8f;
        postEffect = new BuckPostEffect();
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "buck";
        base.SetOn(handler);

        if (handler.HasEffect(this)) return;
        
        handler.SetPlayerParameter("recoil", ref player.BaseRecoil, RECOIL_DELTA);
        handler.SetPlayerParameter("legsDamage", ref player.LegsDamage, LEGS_DELTA);
        handler.SetPlayerParameter("healthMax", ref player.HealthMax, HEALTH_DELTA);
        player.HealHealth(HEALTH_DELTA);
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff();
        if (handler.HasEffect(this)) return;
        
        handler.ClearPlayerParameter("recoil", ref player.BaseRecoil);
        handler.ClearPlayerParameter("legsDamage", ref player.LegsDamage);
        handler.ClearPlayerParameter("healthMax", ref player.HealthMax);
        player.HealHealth(-HEALTH_DELTA);
            
        if (startPostEffect) 
        {
            StartPostEffect();
        }
    }
}