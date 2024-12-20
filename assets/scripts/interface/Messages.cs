using System;
using Godot;
using Godot.Collections;

public class Messages : VBoxContainer, ISavable
{
    public const float HINT_TIMER = 2.5f;
    private const string NONE_CODE = "none";
    
    [Export] public Theme tempTheme;
    
    private readonly Global global = Global.Get();

    private Array<string> currentTaskLinks = [NONE_CODE];
    private bool isNone => (currentTaskLinks.Count == 1) && (currentTaskLinks[0] == NONE_CODE);

    private async void WaitAndDisappear(Label label, float time)
    {
        if (global.player == null)
        {
            await ToSignal(GetTree(), "idle_frame");
        }
        
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

        var tempA = tempLabel.Modulate.a;
        var newColor = Global.Get().Settings.interfaceColor;
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

    public void NewTask(string taskCode, bool showMessage = true, bool clear = true)
    {
        if (clear || isNone)
        {
            currentTaskLinks = [taskCode];
        }
        else
        {
            currentTaskLinks.Insert(0, taskCode);
        }
        
        if (!showMessage) return;

        var taskText = InterfaceLang.GetPhrase("tasks", "tasks", taskCode);
        ShowMessage("newTask", taskText, "messages");
    }

    public void AddTask(string taskCode, bool showMessage = true)
    {
        if (isNone)
        {
            currentTaskLinks = [taskCode];
        }
        else
        {
            currentTaskLinks.Add(taskCode);
        }
        
        if (!showMessage) return;
        
        var taskText = InterfaceLang.GetPhrase("tasks", "tasks", taskCode);
        ShowMessage("addTask", taskText, "messages");
    }
    
    public void DoneTask(string taskCode, bool showMessage = true)
    {
        currentTaskLinks.Remove(taskCode);

        if (currentTaskLinks.Count == 0)
        {
            currentTaskLinks = [NONE_CODE];
        }
        
        if (!showMessage) return;
        
        var taskText = InterfaceLang.GetPhrase("tasks", "tasks", taskCode);
        ShowMessage("doneTask", taskText, "messages");
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
        WaitAndDisappear(tempLabel, timer);
    }

    public void ShowMessage(
        string phraseLink,
        string addMessage,
        string sectionLink = "messages",
        float timer = HINT_TIMER
    )
    {
        var text = InterfaceLang.GetPhrase("inGame", sectionLink, phraseLink);
        var tempLabel = ShowLabel(text + addMessage);
        WaitAndDisappear(tempLabel, timer);
    }

    // Показать "голое" сообщение без поиска его в лангах
    public void ShowMessageRaw(
        string text,
        float timer = HINT_TIMER
    )
    {
        var tempLabel = ShowLabel(text);
        WaitAndDisappear(tempLabel, timer);
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("task") && global.player.MayMove)
        {
            //задачи лежат в файле tasks.json
            var header = InterfaceLang.GetPhrase("tasks", "tasks", "tasksHeader");
            ShowMessageRaw(header, 3);

            foreach (var taskLink in currentTaskLinks)
            {
                var tasks = InterfaceLang.GetPhrase("tasks", "tasks", taskLink);
                ShowMessageRaw(tasks, 3);
            }
        }
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["tasksCount"] = currentTaskLinks.Count;

        for (int i = 0; i < currentTaskLinks.Count; i++)
        {
            saveData.Add($"task{i}", currentTaskLinks[i]);
        }

        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        var tasksCount = Convert.ToInt32(data["tasksCount"]);
        currentTaskLinks.Clear();
        
        for (int i = 0; i < tasksCount; i++)
        {
            currentTaskLinks.Add(data[$"task{i}"].ToString());
        }
    }
}
