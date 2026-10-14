using Godot;

public class SubtitlesTrigger : TriggerBase
{
    [Export] private string code;
    [Export] private NodePath npcPath;
    [Export] private string talkerName;
    [Export] private NodePath audioPlayerPath;
    
    private NPC npc;
    private AudioStreamPlayer3D audioPlayer;
    
    private Subtitles subtitles;
    
    public override void _Ready()
    {
        if (npcPath != null)
        {
            npc = GetNodeOrNull<NPC>(npcPath);
        }
        
        if (audioPlayerPath != null)
        {
            audioPlayer = GetNodeOrNull<AudioStreamPlayer3D>(audioPlayerPath);
        }
        
        subtitles = GetNode<Subtitles>("/root/Main/Scene/canvas/subtitles");
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (npc != null)
        {
            subtitles.SetTalker(npc)
                .LoadSubtitlesFile(code)
                .StartAnimatingText();
        }
        else
        {
            subtitles.SetTalker(audioPlayer, talkerName)
                .LoadSubtitlesFile(code)
                .StartAnimatingText();
        }
        
        base._on_activate_trigger();
    }
}
