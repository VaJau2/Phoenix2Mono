using Godot;

public class EffectIcon : Control
{
    const float SIZE_Y = 30;
    private TextureRect icon;
    private Control timerBack;
    private Label messageLabel;
    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);
        timerBack = GetNode<Control>("back/timer");
        icon = GetNode<TextureRect>("back/icon");
        messageLabel = GetNode<Label>("message");
    }

    public void UpdateTime(float time, float maxTime = 1)
    {
        if (time > 0) {
            float delta = (SIZE_Y * time) / maxTime;
            Vector2 tempSize = timerBack.RectSize;
            tempSize.y = SIZE_Y - delta;
            timerBack.RectSize = tempSize;
        } else {
            QueueFree();
        }
    }

    public void SetData(string code, StreamTexture newIcon)
    {
        icon.Texture = newIcon;
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
