using Godot;

public class ChangeSubtitlesTrigger : ActivateOtherTrigger
{
    [Export] private NodePath npcPath;
    [Export] private string newSubtitlesCode;

    private NPC npc;
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        npc ??= GetNodeOrNull<NPC>(npcPath);
        npc.subtitlesCode = newSubtitlesCode;
        
        base._on_activate_trigger();
    }
    
    public override void SetActive(bool newActive)
    {
        _on_activate_trigger();
        base.SetActive(newActive);
    }
}
