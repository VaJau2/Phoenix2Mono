using Godot;

public class IconWithShadow : TextureRect
{
    private TextureRect shadow;
    
    public override void _Ready()
    {
        shadow = GetNodeOrNull<TextureRect>("shadow");
    }

    public void SetIcon(Texture icon)
    {
        Texture = icon;
        
        if (shadow == null) return;
        
        shadow.Texture = icon;
    }

    public void SetTransparency(float value)
    {
        Modulate = ColorUtils.SetAlpha(Modulate, value);
        
        if (shadow == null) return;
        
        shadow.Modulate = ColorUtils.SetAlpha(shadow.Modulate, value);
    }
}
