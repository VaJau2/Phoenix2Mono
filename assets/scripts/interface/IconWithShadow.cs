using Godot;

public partial class IconWithShadow : TextureRect
{
    private TextureRect shadow;
    
    public override void _Ready()
    {
        shadow = GetNode<TextureRect>("shadow");
    }

    public void SetTexture(Texture2D texture)
    {
        Texture = texture;
        shadow.Texture = texture;
    }
}
