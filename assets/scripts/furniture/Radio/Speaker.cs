using Godot;

public class Speaker : RadioBase
{
    [Export] private NodePath receiverPath;
    private Receiver receiver;

    public override void Initialize()
    {
        InitBase();

        receiver = GetNode<Receiver>(receiverPath);
        receiverPath = null;

        receiver.Connect(nameof(Receiver.ChangeMusicEvent), this, nameof(OnChangeMusic));
        receiver.Connect(nameof(Receiver.ChangeNoiseEvent), this, nameof(OnChangeNoise));

        if (inRoom && !RadioManager.playerInside) RepeaterMode(true);

        if (receiver.noisePlayer.Playing)
        {
            OnChangeMusic();
            OnChangeNoise();
        }
    }

    private void OnChangeMusic()
    {
        musicPlayer.Stream = receiver.musicPlayer.Stream;

        if (receiver.musicPlayer.Playing) musicPlayer.Play(receiver.station.timer);
        else musicPlayer.Stop();
    }

    private void OnChangeNoise()
    {
        noiseDb = receiver.noisePlayer.UnitDb;
        noisePlayer.UnitDb = noiseDb;
        noisePlayer.Stream = receiver.noisePlayer.Stream;

        if (receiver.noisePlayer.Playing) noisePlayer.Play();
        else noisePlayer.Stop();
    }
}
