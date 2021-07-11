using Godot;

public class MusicTrigger: TriggerBase
{
    [Export] public NodePath audiPath;
    [Export] public AudioStreamSample track;
    [Export] private bool Audio3d;
    [Export] private float volumeSpeed = 0.1f;

    private AudioPlayerCommon audi;
    

    public override void _Ready()
    {
        audi = new AudioPlayerCommon(Audio3d, audiPath, this);
        if (IsActive)
        {
            _on_activate_trigger();
        }
    }
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override async void _on_activate_trigger()
    {
        if (!IsActive)
        {
            if (audi.IsPlaying)
            {
                if (volumeSpeed > 0)
                {
                    while (audi.Volume > -8f)
                    {
                        audi.Volume -= volumeSpeed;
                        await Global.Get().ToTimer(0.1f, this);
                    }
                } 
                audi.Stop();
                base._on_activate_trigger();
                return;
            }
        }
        
        audi.Play(track);

        if (volumeSpeed > 0)
        {
            while (audi.Volume < 2)
            {
                audi.Volume += volumeSpeed;
                await Global.Get().ToTimer(0.1f, this);
            }
        }

        base._on_activate_trigger();
    }
}