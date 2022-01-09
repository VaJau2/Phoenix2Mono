using Godot;

public class HintTrigger : TriggerBase
{
    [Export] public string HintSection = "hints";
    [Export] public string HintCode;
    [Export] public bool DiffRacesHints = false;
    [Export] public float hintTime = 2.5f;
    [Export] public bool UseModal;

    private Messages messages;

    private bool modalOn;
    private HintMenu hintModal;
    

    //заменяет все #ui_jump#-значения с кодами кнопок на текущие кнопки из настроек управления
    public static string ReplaceKeys(string message)
    {
        string[] codes = message.Split('#');
        foreach (string tempCode in codes)
        {
            if (!(Global.GetKeyName(tempCode) is string newKey)) continue;
            message = message.Replace("#" + tempCode + "#", newKey);
        }

        return message;
    }

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

    public override void _on_activate_trigger()
    {
        var hintCode = HintCode;
        if (DiffRacesHints)
        {
            hintCode += "_" + Global.RaceToString(Global.Get().playerRace);
        }
        
        string message = InterfaceLang.GetPhrase("inGame", HintSection, hintCode);
        if (message == null) return;
        if (message.Contains("#"))
        {
            message = ReplaceKeys(message);
        }
            
        if (UseModal)
        {
            hintModal.hintMessage = message;
            MenuManager.TryToOpenMenu(hintModal);
        }
        else
        {
            messages.ShowMessageRaw(message, hintTime);
        }
        
        base._on_activate_trigger();
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        
        _on_activate_trigger();
    }
}
