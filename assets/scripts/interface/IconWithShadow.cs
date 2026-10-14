using Godot;

public partial class IconWithShadow : TextureRect
{
    private TextureRect shadow;
    
    public override void _Ready()
    {
        shadow = GetNodeOrNull<TextureRect>("shadow");
    }

    public void SetIcon(Texture2D icon)
    {
        Texture2D = icon;
        
        if (shadow == null) return;
        
        shadow.Texture2D = icon;
    }

    public void SetTransparency(float value)
    {
        Modulate = ColorUtils.SetAlpha(Modulate, value);
        
        if (shadow == null) return;
        
        shadow.Modulate = ColorUtils.SetAlpha(shadow.Modulate, value);
    }
}
