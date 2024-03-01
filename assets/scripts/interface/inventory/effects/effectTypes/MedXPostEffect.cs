public partial class MedXPostEffect : Effect
{
    const float DAMAGE_BLOCK_DELTA = -0.2f;
    private Player player;

    public MedXPostEffect()
    {
        maxTime = 110;
        emotion = "meds_after";
        badEffect = true;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "medX-after";
        base.SetOn(handler);

        if (!handler.HasEffect(this))
        {
            handler.SetPlayerParameter("damageBlock", ref player.BaseDamageBlock, DAMAGE_BLOCK_DELTA);
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff(startPostEffect);
        if (!handler.HasEffect(this))
        {
            handler.ClearPlayerParameter("damageBlock", ref player.BaseDamageBlock);
        }
    }
}