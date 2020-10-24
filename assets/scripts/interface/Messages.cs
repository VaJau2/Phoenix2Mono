using Godot;

public class Messages: VBoxContainer {
    Global global = Global.Get();
    const float HINT_TIMER = 3f;
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

    /// <summary>
    /// Все фразы лежат в lang/inGame.json
    /// </summary>
    public void ShowMessage(string phraseLink, string sectionLink = "messages", float timer = HINT_TIMER) {
        var tempLabel = new Label();
        tempLabel.Autowrap = true;
        tempLabel.Text = InterfaceLang.GetLang("inGame", sectionLink, phraseLink);
        tempLabel.Theme = tempTheme;
        tempLabel.Align = Label.AlignEnum.Right;
        AddChild(tempLabel);
        waitAndDissapear(tempLabel, timer);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("task")) {
            ShowMessage("tasksHeader", "tasks");
            ShowMessage(currentTaskLink, "tasks");
        }
    }
}