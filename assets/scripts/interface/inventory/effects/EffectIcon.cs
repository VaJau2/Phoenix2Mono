using Godot;

public partial class EffectIcon : Control
{
    private Control timer;
    private TextureRect icon;
    private TextureRect shadow;
    private Label messageLabel;
    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);
        
        timer = GetNode<Control>("back/timer");
        icon = GetNode<TextureRect>("back/timer/icon");
        shadow = GetNode<TextureRect>("back/shadow");
        messageLabel = GetNode<Label>("message");
    }

    public void UpdateTime(float time, float maxTime = 1)
    {
        if (time > 0) 
        {
            var ratio = time / maxTime;
            timer.Size = new Vector2(31, ratio * 31);
        } 
        else 
        {
            QueueFree();
        }
    }

    public void SetData(string code, CompressedTexture2D newIcon)
    {
        icon.Texture = newIcon;
        shadow.Texture = newIcon;
        messageLabel.Text = InterfaceLang.GetPhrase("inventory", "effectIconsText", code);
    }

    public void _on_EffectIcon_mouse_entered()
    {
        messageLabel.Visible = true;
    }

    public void _on_EffectIcon_mouse_exited()
    {
        messageLabel.Visible = false;
    }
}
