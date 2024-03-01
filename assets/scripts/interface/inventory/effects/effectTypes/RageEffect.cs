using Godot;

public partial class RageEffect: Effect 
{
    const int DAMAGE_DELTA = 20;
    private Player player;
    public RageEffect()
    {
        maxTime = 60;
        badEffect = true;
        postEffectChance = 0.9f;
        postEffect = new RagePostEffect();
        emotion = "meds";
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "rage";
        base.SetOn(handler);
        
        if (!handler.HasEffect(this)) {
            handler.SetPlayerParameter("damage", ref player.BaseDamage, DAMAGE_DELTA);
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff();
        if (!handler.HasEffect(this)) {
            handler.ClearPlayerParameter("damage", ref player.BaseDamage);
            
            if (startPostEffect) {
                StartPostEffect();
            }
        }
    }
}