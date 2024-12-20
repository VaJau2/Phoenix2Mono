using Godot;
using Godot.Collections;

//терминал - объект, показывающий интерфейс внутри себя
//когда игрок им управляет, другие менюшки не должны открываться
//поэтому он реализует интерфейс менюшки

//работает в нескольких режимах
//которые реализованы через TerminalMode
public class Terminal : StaticBody, IMenu, IInteractable
{
    //при открытии инвентаря, включенный терминал переходит на фон, но не закрывается
    public bool mustBeClosed => false;
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
    public bool isUsing;

    public bool MayInteract => true;
    public string InteractionHintCode => "terminal";

    
    public void OpenMenu()
    {
        isUsing = true;
        player.SetTotalMayMove(false);
        mode.startKeyPressed = true;

        ShowExitHint();
    }

    public void CloseMenu()
    {
        var point = GetNode<InteractionPointManager>("/root/Main/Scene/canvas/pointManager");
        point.ShowSquareAgain();
        
        isUsing = false;
        player.SetTotalMayMove(true);
    }

    private void ShowExitHint()
    {
        var saveNode =  GetNode<SaveNode>("/root/Main/SaveNode");
        var messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        if (saveNode == null || messages == null) return;
        if (saveNode.SavedVariables.Contains("terminalHint")) return;
        
        messages.ShowMessage("5", "hints", 3.5f);
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
        if (isUsing && @event is InputEventKey keyEvent) 
        {
            if (IsExitKey()) 
            {
                StartClosing();
            } 
            else 
            {
                mode.UpdateInput(keyEvent);
            }
        }
    }
    
    public void Interact(PlayerCamera interactor)
    {
        interactor.HideInteractionSquare();
        MenuManager.TryToOpenMenu(this);
    }
}
