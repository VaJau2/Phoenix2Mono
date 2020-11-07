using Godot;

public class MenuBase : Control
{
    Global global = Global.Get();
    public string menuName = "mainMenu";
    protected Label downLabel;
    private bool updatingDownLabel = false;
    private bool downAdded = false;

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

    protected async void UpdateDownLabel() {
        updatingDownLabel = true;
        while(updatingDownLabel) {
            downAdded = !downAdded;
            if (downAdded) {
                downLabel.Text += "_";
            } else {
                downLabel.Text = downLabel.Text.Replace("_", "");
            }
            await global.ToTimer(0.6f, this);
        }
    }

    private async void changeDownLabel() {
        downLabel.PercentVisible = 0;
        while(downLabel.PercentVisible < 1) {
            downLabel.PercentVisible += 0.1f;
            await global.ToTimer(0.01f, this);
        }
    } 

    public void _on_mouse_entered(string section, string messageLink) {
        downLabel.Text = InterfaceLang.GetLang(menuName, section, messageLink);

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
}