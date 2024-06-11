using Godot;

public class MentatsPostEffect : Effect
{
    const float BRIGHTNESS = -0.05f;
    const float CONTRAST = 0.75f;
    const float SATURATION = 0.5f;
    const float PRICE_DELTA = -0.2f;
    private Player player;

    public MentatsPostEffect()
    {
        maxTime = 120;
        badEffect = true;
        emotion = "meds_after";
    }

    private void SetShadersOn(bool on)
    {
        var colorRect = player.GetNode<ColorRect>("/root/Main/Scene/canvas/colorShader");
        if (on)
        {
            var shaders = (ShaderMaterial)colorRect.Material;
            shaders.SetShaderParam("brightness", BRIGHTNESS);
            shaders.SetShaderParam("contrast", CONTRAST);
            shaders.SetShaderParam("saturation", SATURATION);
        }

        colorRect.Visible = on;
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "mentats-after";
        base.SetOn(handler);

        if (!handler.HasEffect(this))
        {
            SetShadersOn(true);
            handler.SetPlayerParameter("priceDelta", ref player.PriceDelta, PRICE_DELTA);
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff();
        if (!handler.HasEffect(this))
        {
            SetShadersOn(false);
            handler.ClearPlayerParameter("priceDelta", ref player.PriceDelta);
        }
    }
}