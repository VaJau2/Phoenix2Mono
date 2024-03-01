using Godot;

public partial class HydraPostEffect: Effect 
{
    private Player player;
    public HydraPostEffect()
    {
        maxTime = 100;
        emotion = "meds_after";
        badEffect = true;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "hydra-after";
        base.SetOn(handler);

        if (!handler.HasEffect(this)) {
            player.FoodCanHeal = false;
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff(startPostEffect);
        if (!handler.HasEffect(this)) {
            player.FoodCanHeal = true;
        }
    }
}