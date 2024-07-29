using Godot;

public struct WarningMessage(string _code, AudioStream _audio, IVoiceMessage _trigger)
{
    public string code = _code;
    public AudioStream audio = _audio;
    public IVoiceMessage trigger = _trigger;

    public void Clear()
    {
        code = null;
        audio = null;
        trigger = null;
    }
}