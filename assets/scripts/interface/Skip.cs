using Godot;

public class Skip : Control
{
    private const float SPEED = 2.0f;
    private const float TIME = 3.0f;

    private bool isActive;
    
    private float timer;
    private ButtonIcon buttonIcon;
    private Label label;
    
    [Signal]
    public delegate void SkipEvent();
    
    public override void _Input(InputEvent @event)
    {
        if (!isActive) return;
        
        if (Input.IsActionJustPressed("jump"))
        {
            if (Visible)
            {
                SetActive(false);
                EmitSignal(nameof(SkipEvent));
            }
            
            Show(!Visible);
        }
    }
    
    public override void _Ready()
    {
        base._Ready();
        buttonIcon = GetNode<ButtonIcon>("icon/button");
        label = GetNode<Label>("label");
        MenuBase.LoadColorForChildren(this);
    }

    public override void _Process(float delta)
    {
        if (Visible)
        {
            label.Text = InterfaceLang.GetPhrase("inGame", "labels", "skip");
        }
        
        if (timer > 0)
        {
            timer -= delta;
            return;
        }
        
        if (label.Modulate.a > 0)
        {
            var alpha = label.Modulate.a - delta * SPEED;
            SetTransparency(alpha);
            return;
        }

        Show(false);
    }

    public void SetActive(bool value)
    {
        isActive = value;
        SetProcess(value);
        if (!isActive) Show(false);
    }
    
    private void Show(bool value)
    {
        var alpha = value ? 1 : 0;
        SetTransparency(alpha);
        Visible = value;
        
        timer = value ? TIME : 0;
        SetProcess(value);
    }

    private void SetTransparency(float value)
    {
        label.Modulate = ColorUtils.SetAlpha(Modulate, value);
        buttonIcon.SetTransparency(value);
    }
}