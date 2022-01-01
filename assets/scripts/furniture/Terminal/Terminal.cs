using Godot;
using Godot.Collections;

//терминал - объект, показывающий интерфейс внутри себя
//когда игрок им управляет, другие менюшки не должны открываться
//поэтому он реализует интерфейс менюшки

//работает в нескольких режимах
//которые реализованы через TerminalMode
public class Terminal : StaticBody, IMenu
{
    //при открытии инвентаря, включенный терминал переходит на фон, но не закрывается
    public bool mustBeClosed {get => false;}
    [Export]
    public string startPhraseCode = "personal";
    [Export]
    public string startCommand = ">";
    [Export]
    public string[] files;
    [Export] 
    private Array<NodePath> doorsPath = new Array<NodePath>();
    
    public Array<FurnDoor> doors = new Array<FurnDoor>();
 
    Player player => Global.Get().player;
    
    public TerminalMode mode;
    public bool isUsing = false;

    
    public void OpenMenu()
    {
        isUsing = true;
        player.MayMove = false;
        player.MayRotateHead = false;
        player.Camera.isUpdating = false;
        mode.startKeyPressed = true;

        ShowExitHint();
    }

    public void CloseMenu()
    {
        isUsing = false;
        player.MayMove = true;
        player.Camera.isUpdating = true;
        player.MayRotateHead = true;
    }

    private void ShowExitHint()
    {
        var saveNode =  GetNode<SaveNode>("/root/Main/SaveNode");
        var messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        if (saveNode == null || messages == null) return;
        if (saveNode.SavedVariables.Contains("terminalHint")) return;
        
        string message = InterfaceLang.GetPhrase("inGame", "hints", "5");
        message = HintTrigger.ReplaceKeys(message);
        messages.ShowMessageRaw(message, 3.5f);
        saveNode.SavedVariables["terminalHint"] = true;
    }

    private bool IsExitKey()
    {
        return Input.IsActionJustPressed("inventory");
    }

    public async void StartClosing()
    {
        //закрытие терминала нужно переместить в конец кадра
        //тк инвентарь проверяется на открытие сразу после закрытия терминала
        await ToSignal(GetTree(), "idle_frame");
        MenuManager.CloseMenu(this);
    }

    public async void ChangeMode(TerminalMode newMode)
    {
        mode.ClearOutput();
        mode = newMode;
        await ToSignal(GetTree(), "idle_frame");
        mode.LoadMode();
    }

    public override async void _Ready()
    {
        foreach (var doorPath in doorsPath)
        {
            var tempDoor = GetNode<FurnDoor>(doorPath);
            if (IsInstanceValid(tempDoor))
            {
                doors.Add(tempDoor);
            }
        }
        
        mode = new TerminalUsualMode(this);
        await ToSignal(GetTree(), "idle_frame");
        mode.LoadMode();
    }

    public override void _Input(InputEvent @event)
    {
        if (isUsing && @event is InputEventKey) {
            var keyEvent = @event as InputEventKey;
            if (IsExitKey()) {
                StartClosing();
            } else {
                mode.UpdateInput(keyEvent);
            }
        }
    }
}
