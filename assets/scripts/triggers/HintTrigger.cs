using Godot;

public partial class HintTrigger : TriggerBase
{
    [Export] public string HintSection = "hints";
    [Export] public string HintCode;
    [Export] public bool DiffRacesHints = false;
    [Export] public float hintTime = 2.5f;
    [Export] public bool UseModal;

    private Messages messages;

    private bool modalOn;
    private HintMenu hintModal;

    public override void _Ready()
    {
        if (UseModal)
        {
            hintModal = GetNode<HintMenu>("/root/Main/Scene/canvas/hintModal");
        }
        else
        {
            messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        }

        SetProcess(false);
    }

    public override void OnActivateTrigger()
    {
        var hintCode = HintCode;
        if (DiffRacesHints)
        {
            hintCode += "_" + Global.RaceToString(Global.Get().playerRace);
        }
        
        string message = InterfaceLang.GetPhrase("inGame", HintSection, hintCode);
        if (message == null) return;

        if (UseModal)
        {
            hintModal.hintMessage = message;
            MenuManager.TryToOpenMenu(hintModal);
        }
        else
        {
            messages.ShowMessageRaw(message, hintTime);
        }
        
        base.OnActivateTrigger();
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        
        OnActivateTrigger();
    }
}
