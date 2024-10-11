using Godot;

public class NpcInteraction: Node
{
    private NPC npc;
    private DialogueMenu dialogueMenu;
    private Subtitles subtitles;
    private TriggerBase customInteractionTrigger;
    
    private bool IsUseCustomTrigger => !string.IsNullOrEmpty(npc.customHintCode)
                                       && customInteractionTrigger is { IsActive: true };

    public override void _Ready()
    {
        npc = GetParent<NPC>();
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        subtitles = GetNode<Subtitles>("/root/Main/Scene/canvas/subtitles");
        
        if (!string.IsNullOrEmpty(npc.customHintCode) && npc.customInteractionTriggerPath != null)
        {
            customInteractionTrigger = GetNode<TriggerBase>(npc.customInteractionTriggerPath);
        }
    }

    public bool GetMayInteract()
    {
        if (IsUseCustomTrigger)
        {
            return customInteractionTrigger.IsActive;
        }

        if (npc.Health <= 0)
        {
            return true;
        }
        
        if (!string.IsNullOrEmpty(npc.dialogueCode))
        {
            return !dialogueMenu.MenuOn;
        }

        if (!string.IsNullOrEmpty(npc.subtitlesCode))
        {
            return !subtitles.IsAnimatingText;
        }

        return false;
    }
    
    public string GetInteractionHintCode()
    {
        if (IsUseCustomTrigger) return npc.customHintCode;
        return npc.Health > 0 ? "talk" : "search";
    }

    public void Interact()
    {
        if (customInteractionTrigger is { IsActive: true })
        {
            customInteractionTrigger._on_activate_trigger();
            return;
        }
        
        if (npc.Health <= 0)
        {
            npc.ChestHandler.Open();
            return;
        }

        if (!string.IsNullOrEmpty(npc.dialogueCode))
        {
            dialogueMenu.StartTalkingTo(npc);
        }

        if (!string.IsNullOrEmpty(npc.subtitlesCode))
        {
            subtitles.SetTalker(npc)
                .LoadSubtitlesFile(npc.subtitlesCode)
                .StartAnimatingText();
        }
    }
}
