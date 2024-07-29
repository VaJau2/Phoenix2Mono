using Godot;
using System;
using System.Collections.Generic;

public class VoiceMessageTrigger : TriggerBase, IVoiceMessage
{
    [Export] private List<string> messages;
    private WarningManager warningManager;

    public override void _Ready()
    {
        warningManager = GetNode<WarningManager>("/root/Main/Scene/Warning Manager");
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (body is not Player) return;
        if (warningManager.IsMessagePlaying) return;
        
        switch (messages.Count)
        {
            case 0:
                return;
            
            case 1:
                SendMessage(0);
                break;
            
            default:
            {
                var rand = new Random();
                var index = rand.Next(0, messages.Count - 1);
                SendMessage(index);
                break;
            }
        }
        
        if (!DeleteAfterTrigger) SetActive(false);
    }

    public void OnMessageFinished()
    {
        _on_activate_trigger();
    }
    
    public void Connect()
    {
        if (!DeleteAfterTrigger) return;
        warningManager.Connect(nameof(WarningManager.MessageFinishedEvent), this, nameof(OnMessageFinished));
    }

    private void SendMessage(int index)
    {
        if (warningManager.message.code == messages[index]) return;
        warningManager.SendMessage(messages[index], this);
    }
}