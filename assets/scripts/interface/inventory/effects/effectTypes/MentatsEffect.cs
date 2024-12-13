using Godot;

namespace Effects;

public class MentatsEffect : Effect
{
    const float PRICE_DELTA = 0.25f;
    const float MANA_DELTA = -0.6f;

    private Player player;
    private Global global => Global.Get();
    private float tempAlpha;

    public MentatsEffect()
    {
        maxTime = 60;
        badEffect = true;
        postEffectChance = 0.8f;
        postEffect = new MentatsPostEffect();
    }

    private void SetShadersOn(bool on)
    {
        var colorRect = player.GetNode<ColorRect>("/root/Main/Scene/canvas/fisheyeShader");
        var shader = (ShaderMaterial)colorRect.Material;

        if (on)
        {
            SetShaderOn(colorRect, shader);
        }
        else
        {
            SetShaderOff(colorRect, shader);
        }
    }

    public override void SetOn(EffectHandler handler)
    {
        player = Global.Get().player;
        iconName = "mentats";
        base.SetOn(handler);

        if (!handler.HasEffect(this))
        {
            SetShadersOn(true);
            handler.SetPlayerParameter("priceDelta", ref player.PriceDelta, PRICE_DELTA);
            if (global.playerRace == Race.Unicorn)
            {
                Player_Unicorn unicorn = player as Player_Unicorn;
                handler.SetPlayerParameter("manaDelta", ref unicorn.ManaDelta, MANA_DELTA);
            }
        }
    }

    public override void SetOff(bool startPostEffect = true)
    {
        base.SetOff();
        if (!handler.HasEffect(this))
        {
            SetShadersOn(false);
            handler.ClearPlayerParameter("priceDelta", ref player.PriceDelta);
            if (global.playerRace == Race.Unicorn)
            {
                Player_Unicorn unicorn = player as Player_Unicorn;
                handler.ClearPlayerParameter("manaDelta", ref unicorn.ManaDelta);
            }

            if (startPostEffect)
            {
                StartPostEffect();
            }
        }
    }

    private async void SetShaderOn(ColorRect colorRect, ShaderMaterial shader)
    {
        colorRect.Visible = true;

        while (tempAlpha < 1)
        {
            shader.SetShaderParam("alpha", tempAlpha);
            tempAlpha += 0.02f;
            await player.ToSignal(player.GetTree(), "idle_frame");
        }
    }

    private async void SetShaderOff(ColorRect colorRect, ShaderMaterial shader)
    {
        while (tempAlpha > 0)
        {
            shader.SetShaderParam("alpha", tempAlpha);
            tempAlpha -= 0.02f;
            await player.ToSignal(player.GetTree(), "idle_frame");
        }

        colorRect.Visible = false;
    }
}