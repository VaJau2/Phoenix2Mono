using Godot;

public class MenuBase : Control
{
    Global global = Global.Get();
    public string menuName = "mainMenu";
    protected Label downLabel;
    private bool downAdded = false;
    private float downLabelTimer;
    private const float DOWN_LABEL_TIME = 0.6f;

    private string tempSection, tempPhrase;

    public override void _Ready()
    {
        downLabel = GetNode<Label>("down_label");
    }
    
    public virtual void SetMenuVisible(bool animate = false)
    {
        Visible = true;
    }

    public virtual void SoundClick() {}

    private async void changeDownLabel() {
        downLabel.PercentVisible = 0;
        while(downLabel.PercentVisible < 1) {
            downLabel.PercentVisible += 0.1f;
            await global.ToTimer(0.01f, this);
        }
    } 

    public void _on_mouse_entered(string section, string messageLink) {
        downLabel.Text = InterfaceLang.GetPhrase(menuName, section, messageLink);

        if (downAdded) {
            downLabel.Text += "_";
        }
        changeDownLabel();

        tempSection = section;
        tempPhrase = messageLink;
    }

    public void ReloadMouseEntered()
    {
        _on_mouse_entered(tempSection, tempPhrase);
    }

    public void _on_mouse_exited() {
        if (downAdded) {
            downLabel.Text = "_";
        } else {
            downLabel.Text = "";
        }
    }

    public override void _Process(float delta)
    {
        if (downLabelTimer > 0) {
            downLabelTimer -= delta;
        } else {
            downAdded = !downAdded;
            if (downAdded) {
                downLabel.Text += "_";
            } else {
                downLabel.Text = downLabel.Text.Replace("_", "");
            }
            downLabelTimer = DOWN_LABEL_TIME;
        }
    }
}