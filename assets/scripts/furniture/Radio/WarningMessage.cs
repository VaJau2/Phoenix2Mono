using Godot;

public struct WarningMessage(string _code, VoiceMessageTrigger _trigger)
{
    public string code = _code;
    public VoiceMessageTrigger trigger = _trigger;

    public void Clear()
    {
        code = null;
        trigger = null;
    }
}