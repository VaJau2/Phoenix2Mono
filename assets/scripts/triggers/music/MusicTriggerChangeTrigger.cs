using Godot;

public class MusicTriggerChangeTrigger : TriggerBase
{
    [Export] private NodePath musicTriggerPath;
    [Export] public AudioStream newTrack;
    [Export] public AudioStream newOtherTrack;
    [Export] public bool changeActive;
    [Export] public bool changeActiveTo;

    private MusicTrigger musicTrigger;

    public override void _Ready()
    {
        base._Ready();
        musicTrigger = GetNode<MusicTrigger>(musicTriggerPath);
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        musicTrigger.track = newTrack;
        musicTrigger.otherTrack = newOtherTrack;
        if (changeActive)
        {
            musicTrigger.SetActive(changeActiveTo);
        }
        base._on_activate_trigger();
    }
}
