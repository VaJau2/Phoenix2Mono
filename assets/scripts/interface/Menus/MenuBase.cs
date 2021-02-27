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

        LoadColorForChildren(this);
    }

    //загрузка цвета для интерфейса из настроек
    public static void LoadColorForChildren(Node node)
    {
        if (node.GetChildCount() == 0) {
            return;
        }
        foreach(var child in node.GetChildren()) {
            if (child is CanvasItem) {
                var canvasChild = child as CanvasItem;
                if (!canvasChild.IsInGroup("ignore_color")) {
                    float tempA = canvasChild.Modulate.a;
                    Color newColor = Global.Get().Settings.interfaceColor;
                    canvasChild.Modulate = new Color (
                        newColor.r,
                        newColor.g,
                        newColor.b,
                        tempA
                    );
                }
                if (canvasChild.GetChildCount() > 0) {
                    LoadColorForChildren(canvasChild);
                }
            }
        }
    }
    
    public virtual void SetMenuVisible(bool animate = false)
    {
        Visible = true;
    }

    public virtual void SoundClick() {}

    public virtual void loadInterfaceLanguage() {}

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