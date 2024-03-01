using Godot;

public partial class Speaker : RadioBase
{
    [Export] private NodePath receiverPath;
    private Receiver receiver;

    public override void Initialize()
    {
        InitBase();

        if (InRoom) RepeaterMode(true);

        if (receiverPath != null)
        {
            receiver = GetNodeOrNull<Receiver>(receiverPath);
            receiverPath = null;
        }
        
        if (receiver != null)
        {
            receiver.ChangeOnline += OnChangeOnline;
            receiver.MusicChanged += OnChangeMusic;
            receiver.ChangeNoise += OnChangeNoise;
            OnChangeNoise(receiver.NoisePlayer.Stream);
            OnChangeMusic(receiver.MusicPlayer.Stream);
        }
        else
        {
            warningManager.MessageSent += OnChangeMusic;
            OnChangeMusic(warningManager.message);
        }
    }

    private void OnChangeOnline(RadioBase radio)
    {
        if (warningManager != null)
        {
            if (warningManager.IsMessagePlaying)
            {
                MusicPlayer.Play(warningManager.timer);
                return;
            }
            
            MusicPlayer.Stop();
        }
        
        if (receiver != null)
        {
            if (receiver.MusicPlayer.Playing) MusicPlayer.Play(receiver.Radiostation.timer);
            else MusicPlayer.Stop();
        }
    }
    
    private void OnChangeMusic(AudioStream stream)
    {
        MusicPlayer.Stream = stream;
        
        if (warningManager is { IsMessagePlaying: true })
        {
            MusicPlayer.Play(warningManager.timer);
            return;
        }
        
        if (receiver != null)
        {
            MusicPlayer.Play(receiver.Radiostation.timer);
        }
    }

    private void OnChangeNoise(AudioStream stream)
    {
        noiseDb = receiver.NoisePlayer.VolumeDb;
        NoisePlayer.VolumeDb = noiseDb;
        NoisePlayer.Stream = stream;

        if (receiver.NoisePlayer.Playing) NoisePlayer.Play();
        else NoisePlayer.Stop();
    }
}
