namespace Effects;

public class DetoxineEffect: Effect 
{
    private Player player;
    public DetoxineEffect()
    {
        maxTime = 0;
        badEffect = false;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "detoxine";
        base.SetOn(handler);
        
        handler.ClearEffects();
    }
}