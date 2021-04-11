using Godot;

public class HintTrigger : TriggerBase
{
    [Export] public string HintSection = "hints";
    [Export] public string HintCode;
    [Export] public bool DiffRacesHints = false;
    [Export] public float hintTime = 2.5f;

    private Messages messages;

    //заменяет все #ui_jump#-значения с кодами кнопок на текущие кнопки из настроек управления
    private static string ReplaceKeys(string message)
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
        messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
    }

    public override void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        
        var hintCode = HintCode;
        if (DiffRacesHints)
        {
            hintCode += "_" + Global.RaceToString(Global.Get().playerRace);
        }
        
        string message = InterfaceLang.GetPhrase("inGame", HintSection, hintCode);
        if (message != null)
        {
            if (message.Contains("#"))
            {
                message = ReplaceKeys(message);
            }

            messages.ShowMessageRaw(message, hintTime);
        }

        base._on_body_entered(body);
    }
}
