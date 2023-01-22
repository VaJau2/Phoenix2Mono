using Godot;
using System;

public class Speaker : RadioBase
{
    [Export] NodePath receiverPath;
    Receiver receiver;

    public override void Initialize()
    {
        InitBase();

        receiver = GetNode<Receiver>(receiverPath);
        receiver.Connect(nameof(Receiver.ChangeMusicEvent), this, nameof(OnChangeMusic));
        receiver.Connect(nameof(Receiver.ChangeNoiseEvent), this, nameof(OnChangeNoise));

        if (receiver.musicPlayer != null)
        {
            OnChangeMusic();
            OnChangeNoise();
        }

        if (inRoom && !radioController.playerInside) RepeaterMode(true);
    }

    void OnChangeMusic()
    {
        musicPlayer.Stream = receiver.musicPlayer.Stream;
        musicPlayer.UnitDb = receiver.musicPlayer.UnitDb;
        if (receiver.isOn) musicPlayer.Play(receiver.station.timer);
    }

    void OnChangeNoise()
    {
        noisePlayer.Stream = receiver.noisePlayer.Stream;
        noisePlayer.UnitDb = receiver.noisePlayer.UnitDb;
        if (receiver.isOn) noisePlayer.Play();
    }
}
