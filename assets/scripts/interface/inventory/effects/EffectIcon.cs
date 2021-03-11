using Godot;

public class EffectIcon : Control
{
    const float SIZE_Y = 30;
    private TextureRect icon;
    private Control timerBack;
    public override void _Ready()
    {
        MenuBase.LoadColorForChildren(this);
        timerBack = GetNode<Control>("back/timer");
        icon = GetNode<TextureRect>("back/icon");
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

    public void SetIcon(StreamTexture newIcon)
    {
        icon.Texture = newIcon;
    }
}
