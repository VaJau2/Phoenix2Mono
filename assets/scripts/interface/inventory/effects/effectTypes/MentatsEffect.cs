using Godot;

public class MentatsEffect: Effect 
{
    const float BRIGHTNESS = 0.1f;
    const float CONTRAST = 1.25f;
    const float SATURATION = 1.25f;
    const float PRICE_DELTA = 0.25f;
    const float MANA_DELTA = -0.6f;
    private Player player;
    private Global global => Global.Get();
    public MentatsEffect()
    {
        maxTime = 60;
        badEffect = true;
        postEffectChance = 0.8f;
        postEffect = new MentatsPostEffect();
    }

    private void SetShadersOn(bool on) 
    {
        var colorRect = player.GetNode<ColorRect>("/root/Main/Scene/canvas/colorShader");
        if (on) {
            ShaderMaterial shaders = (ShaderMaterial)colorRect.Material;
            shaders.SetShaderParam("brightness", BRIGHTNESS);
            shaders.SetShaderParam("contrast", CONTRAST);
            shaders.SetShaderParam("saturation", SATURATION);
        }
        
        colorRect.Visible = on;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "mentats";
        base.SetOn(handler);

        if (!handler.HasEffect(this)) {
            SetShadersOn(true);
            handler.SetPlayerParameter("priceDelta", ref player.PriceDelta, PRICE_DELTA);
            if (global.playerRace == Race.Unicorn) {
                Player_Unicorn unicorn = player as Player_Unicorn;
                handler.SetPlayerParameter("manaDelta", ref unicorn.ManaDelta, MANA_DELTA);
            }
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff();
        if (!handler.HasEffect(this)) {
            SetShadersOn(false);
            handler.ClearPlayerParameter("priceDelta", ref player.PriceDelta);
            if (global.playerRace == Race.Unicorn) {
                Player_Unicorn unicorn = player as Player_Unicorn;
                handler.ClearPlayerParameter("manaDelta", ref unicorn.ManaDelta);
            }
            
            if (startPostEffect) {
                StartPostEffect();
            }
        }
    }
}