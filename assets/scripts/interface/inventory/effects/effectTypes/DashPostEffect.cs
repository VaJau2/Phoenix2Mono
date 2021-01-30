using Godot;

public class DashPostEffect: Effect 
{
    const int SPEED_DELTA = -5;
    private Player player;
    public DashPostEffect()
    {
        maxTime = 100;
        emotion = "meds_after";
        badEffect = true;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "dash-after";
        base.SetOn(handler);

        if (!handler.HasEffect(this)) {
            handler.SetPlayerParameter("speed", ref player.BaseSpeed, SPEED_DELTA);
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff(startPostEffect);
        if (!handler.HasEffect(this)) {
            handler.ClearPlayerParameter("speed", ref player.BaseSpeed);
        }
    }
}