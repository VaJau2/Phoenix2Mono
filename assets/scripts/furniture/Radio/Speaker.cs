using Godot;

public class Speaker : RadioBase
{
    [Export] private NodePath receiverPath;
    private Receiver receiver;

    public override void Initialize()
    {
        InitBase();

        if (InRoom && !RadioManager.playerInside) RepeaterMode(true);

        if (receiverPath != null)
        {
            receiver = GetNodeOrNull<Receiver>(receiverPath);
            receiverPath = null;
        }
        
        if (receiver != null)
        {
            receiver.Connect(nameof(ChangeOnline), this, nameof(OnChangeOnline));
            receiver.Connect(nameof(Receiver.ChangeMusicEvent), this, nameof(OnChangeMusic));
            receiver.Connect(nameof(Receiver.ChangeNoiseEvent), this, nameof(OnChangeNoise));
            OnChangeNoise(receiver.NoisePlayer.Stream);
            OnChangeMusic(receiver.MusicPlayer.Stream);
        }
        else
        {
            warningManager.Connect(nameof(WarningManager.SendMessageEvent), this, nameof(OnChangeMusic));
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
        noiseDb = receiver.NoisePlayer.UnitDb;
        NoisePlayer.UnitDb = noiseDb;
        NoisePlayer.Stream = stream;

        if (receiver.NoisePlayer.Playing) NoisePlayer.Play();
        else NoisePlayer.Stop();
    }
}
