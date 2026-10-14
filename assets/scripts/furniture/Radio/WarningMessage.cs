public class WarningMessage
{
    public string code;
    public VoiceMessageTrigger trigger;

    public WarningMessage(string _code, VoiceMessageTrigger _trigger)
    {
        code = _code;
        trigger = _trigger;
    }
}
