using Godot;
using System;
using System.Collections.Generic;

public class VoiceMessageTrigger : TriggerBase, IVoiceMessage
{
    [Export] private bool ignorable;
    [Export] private bool random;
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
        if (ignorable && warningManager.IsMessagePlaying) return;
        
        switch (messages.Count)
        {
            case 0:
                return;
            
            case 1:
                SendMessage(0);
                break;
            
            default:
            {
                if (random)
                {
                    var rand = new Random();
                    var index = rand.Next(0, messages.Count - 1);
                    SendMessage(index);
                }
                else
                {
                    for (var i = 0; i < messages.Count; i++)
                    {
                        SendMessage(i);
                    }
                }
                break;
            }
        }
        
        if (!DeleteAfterTrigger) SetActive(false);
    }

    public void OnMessageFinished()
    {
        base._on_activate_trigger();
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