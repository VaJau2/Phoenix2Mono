using Godot;

public class DashEffect: Effect 
{
    const int SPEED_DELTA = 10;
    const float GAME_SPEED = 0.6f;
    private Player player;
    public DashEffect()
    {
        maxTime = 10;
        badEffect = true;
        postEffectChance = 0.9f;
        postEffect = new DashPostEffect();
        emotion = "meds";
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "dash";
        base.SetOn(handler);

        if (!handler.HasEffect(this)) {
            handler.SetPlayerParameter("speed", ref player.BaseSpeed, SPEED_DELTA);
            Engine.TimeScale = GAME_SPEED;
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff(startPostEffect);
        if (!handler.HasEffect(this)) {
            handler.ClearPlayerParameter("speed", ref player.BaseSpeed);
            Engine.TimeScale = 1f;
            
            if (startPostEffect) {
                StartPostEffect();
            }
        }
    }
}