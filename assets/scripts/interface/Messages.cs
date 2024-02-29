using Godot;
using Godot.Collections;

public class Messages : VBoxContainer, ISavable
{
    Global global = Global.Get();
    public const float HINT_TIMER = 2.5f;
    [Export] public Theme tempTheme;

    private string currentTaskLink = "none";

    private async void waitAndDisappear(Label label, float time)
    {
        await global.ToTimer(time, null, true);
        var tempA = label.GetColor("font_color").a;
        
        while (tempA > 0)
        {
            tempA -= 0.1f;
            label.AddColorOverride("font_color", new Color(1, 1, 1, tempA));
            label.AddColorOverride("font_color_shadow", new Color(0, 0, 0, tempA));
            await global.ToTimer(0.05f, null, true);
        }

        label.QueueFree();
    }

    private Label ShowLabel(string text)
    {
        var tempLabel = new Label();

        float tempA = tempLabel.Modulate.a;
        Color newColor = Global.Get().Settings.interfaceColor;
        tempLabel.Modulate = new Color(
            newColor.r,
            newColor.g,
            newColor.b,
            tempA
        );

        tempLabel.Autowrap = true;
        tempLabel.Text = text;
        tempLabel.Theme = tempTheme;
        tempLabel.Align = Label.AlignEnum.Left;
        AddChild(tempLabel);
        return tempLabel;
    }

    public void ChangeTaskCode(string newTaskCode, bool showMessage = true)
    {
        currentTaskLink = newTaskCode;
        if (!showMessage) return;

        string taskText = InterfaceLang.GetPhrase("tasks", "tasks", newTaskCode);
        ShowMessage("newTask", taskText, "messages");
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
        waitAndDisappear(tempLabel, timer);
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
        waitAndDisappear(tempLabel, timer);
    }

    // Показать "голое" сообщение без поиска его в лангах
    public void ShowMessageRaw(
        string text,
        float timer = HINT_TIMER
    )
    {
        var tempLabel = ShowLabel(text);
        waitAndDisappear(tempLabel, timer);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("task") && global.player.MayMove)
        {
            //задачи лежат в файле tasks.json
            string header = InterfaceLang.GetPhrase("tasks", "tasks", "tasksHeader");
            string tasks = InterfaceLang.GetPhrase("tasks", "tasks", currentTaskLink);
            ShowMessageRaw(header, 3);
            ShowMessageRaw(tasks, 3);
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "task", currentTaskLink }
        };
    }

    public void LoadData(Dictionary data)
    {
        currentTaskLink = data["task"].ToString();
    }
}