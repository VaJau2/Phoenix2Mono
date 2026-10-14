using Godot;

public partial class ColorUtils
{
    public static Color SetAlpha(Color color, float value)
    {
         color.a = value;
         return color;
    }
}