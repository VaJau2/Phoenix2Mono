using Godot;

public class MedXEffect: Effect 
{
   const float DAMAGE_BLOCK_DELTA = 0.2f;

    private Player player;
    public MedXEffect()
    {
        maxTime = 60;
        badEffect = true;
        postEffectChance = 0.6f;
        postEffect = new MedXPostEffect();
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "medX";
        base.SetOn(handler);
        
        if (!handler.HasEffect(this)) {
            handler.SetPlayerParameter("damageBlock", ref player.BaseDamageBlock, DAMAGE_BLOCK_DELTA);
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff();
        if (!handler.HasEffect(this)) {
            handler.ClearPlayerParameter("damageBlock", ref player.BaseDamageBlock);
            
            if (startPostEffect) {
                StartPostEffect();
            }
        }
    }
}