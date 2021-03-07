using Godot;

//терминал - объект, показывающий интерфейс внутри себя
//когда игрок им управляет, другие менюшки не должны открываться
//поэтому он реализует интерфейс менюшки

//работает в нескольких режимах
//которые реализованы через TerminalMode
public class Terminal : StaticBody, IMenu
{
    public bool mustBeClosed {get => false;}
    [Export]
    public string startPhraseCode = "personal";
    [Export]
    public string startCommand = ">";
    [Export]
    public string[] files;
 
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
    }

    public void CloseMenu()
    {
        isUsing = false;
        player.MayMove = true;
        player.Camera.isUpdating = true;
        player.MayRotateHead = true;
    }

    private bool IsExitKey(uint keyCode)
    {
        return keyCode == (uint)KeyList.Tab;
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

    public void InitiateScript(string scriptName, string parameter)
    {
        var scriptType = System.Type.GetType(scriptName);
        var scriptObj = System.Activator.CreateInstance(scriptType) as ITerminalScript;
        scriptObj.initiate(this, parameter);
    }


    public override async void _Ready()
    {
        mode = new TerminalUsualMode(this);
        await ToSignal(GetTree(), "idle_frame");
        mode.LoadMode();
    }

    public override void _Input(InputEvent @event)
    {
        if (isUsing && @event is InputEventKey) {
            var keyEvent = @event as InputEventKey;
            if (IsExitKey(keyEvent.Scancode)) {
                StartClosing();
            } else {
                mode.UpdateInput(keyEvent);
            }
        }
    }
}