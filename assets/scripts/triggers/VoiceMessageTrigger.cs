using Godot;
using System.Collections.Generic;

public class VoiceMessageTrigger : TriggerBase
{
    [Export] private List<AudioStream> messages;

    private AudioPlayerCommon messagePlayer;

    public override void _Ready()
    {
        messagePlayer = new AudioPlayerCommon("Message Player", this);
        messagePlayer.Connect("finished", this, nameof(OnMessageFinished));
    }

    public void _on_body_entered(Node body = null)
    {
        if (!IsActive) return;
        if (body is not Player) return;

        if (messages.Count == 1)
        {
            messagePlayer.Play(messages[0]);
        }
    }

    public void OnMessageFinished()
    {
        base._on_activate_trigger();
    }
}