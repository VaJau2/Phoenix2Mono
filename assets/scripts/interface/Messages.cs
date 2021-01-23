using Godot;

public class Messages: VBoxContainer {
    Global global = Global.Get();
    const float HINT_TIMER = 2.5f;
    [Export]
    public Theme tempTheme;

    public string currentTaskLink = "none";

    private async void waitAndDissapear(Label label, float time) {
        await global.ToTimer(time);
        var tempA = label.GetColor("font_color").a;
        while(tempA > 0) {
            tempA -= 0.1f; 
            label.AddColorOverride("font_color", new Color(1,1,1,tempA));
            await global.ToTimer(0.05f);
        }
        label.QueueFree();
    }

    private Label ShowLabel(string text) 
    {
        var tempLabel = new Label();
        tempLabel.Autowrap = true;
        tempLabel.Text = text;
        tempLabel.Theme = tempTheme;
        tempLabel.Align = Label.AlignEnum.Left;
        AddChild(tempLabel);
        return tempLabel;
    }

    // Все фразы лежат в lang/inGame.json
    public void ShowMessage(
        string phraseLink,
        string sectionLink = "messages", 
        float timer = HINT_TIMER
    ) 
    {
        string text = InterfaceLang.GetPhrase("inGame", sectionLink, phraseLink);
        var tempLabel = ShowLabel(text);
        waitAndDissapear(tempLabel, timer);
    }

    public void ShowMessage(
        string phraseLink, 
        string addMessage, 
        string sectionLink = "messages", 
        float timer = HINT_TIMER
    )
    {
        string text = InterfaceLang.GetPhrase("inGame", sectionLink, phraseLink);
        var tempLabel = ShowLabel(text + addMessage);
        waitAndDissapear(tempLabel, timer);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("task")) {
            ShowMessage("tasksHeader", "tasks", 3);
            ShowMessage(currentTaskLink, "tasks", 3);
        }
    }
}