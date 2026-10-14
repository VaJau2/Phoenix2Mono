using Godot;

public class ColorUtils
{
    public static Color SetAlpha(Color color, float value)
    {
         color.A = value;
         return color;
    }
}