using Godot;
using System;
using System.Collections.Generic;

public class VoiceMessageTrigger : TriggerBase, IVoiceMessage
{
    [Export] private List<string> messages;
    protected WarningManager WarningManager;

    public override void _Ready()
    {
        WarningManager = GetNode<WarningManager>("/root/Main/Scene/Warning Manager");
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (body is not Player) return;
        
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
    }

    protected virtual void OnMessageFinished()
    {
        WarningManager.Disconnect
        (
            nameof(WarningManager.MessageFinishedEvent), 
            this, 
            nameof(OnMessageFinished)
        );
        
        SetActive(false);
        _on_activate_trigger();
    }
    
    public void Connect()
    {
        SetActive(false);
        WarningManager.Connect
        (
            nameof(WarningManager.MessageFinishedEvent), 
            this, 
            nameof(OnMessageFinished)
        );
    }

    private void SendMessage(int index)
    {
        if (WarningManager.Message?.code == messages[index]) return;
        WarningManager.SendMessage(messages[index], this);
    }
}
