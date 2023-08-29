using Godot;

public class IconWithShadow : TextureRect
{
    private TextureRect shadow;
    
    public override void _Ready()
    {
        shadow = GetNode<TextureRect>("shadow");
    }

    public new void SetTexture(Texture texture)
    {
        Texture = texture;
        shadow.Texture = texture;
    }
}
