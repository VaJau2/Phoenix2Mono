using Godot;
using System;

public class Speaker : StaticBody
{
    [Export] NodePath radioPath;
    Radio radio;

    AudioStreamPlayer3D musicPlayer;
    AudioStreamPlayer3D noisePlayer;

    public override void _Ready()
    {
        musicPlayer = GetNode<AudioStreamPlayer3D>("Music Player");
        noisePlayer = GetNode<AudioStreamPlayer3D>("Noise Player");

        radio = GetNode<Radio>(radioPath);
        radio.OnChangeRepeaterMode += OnChangeRepeaterMode;
        radio.OnChangeMusic += OnChangeMusic;
        radio.OnChangeNoise += OnChangeNoise;

        if (radio.musicPlayer != null)
        {
            OnChangeMusic();
            OnChangeNoise();
        }
    }

    public override void _ExitTree()
    {
        radio.OnChangeRepeaterMode -= OnChangeRepeaterMode;
        radio.OnChangeMusic -= OnChangeMusic;
        radio.OnChangeNoise -= OnChangeNoise;
    }

    void OnChangeMusic()
    {
        musicPlayer.Stream = radio.musicPlayer.Stream;
        musicPlayer.UnitDb = radio.musicPlayer.UnitDb;
        musicPlayer.MaxDb = radio.musicPlayer.MaxDb;
        if (radio.isOn) musicPlayer.Play(radio.station.timer);
    }

    void OnChangeNoise()
    {
        noisePlayer.Stream = radio.noisePlayer.Stream;
        noisePlayer.UnitDb = radio.noisePlayer.UnitDb;
        noisePlayer.MaxDb = radio.noisePlayer.MaxDb;
        if (radio.isOn) noisePlayer.Play();
    }

    void OnChangeRepeaterMode()
    {
        var transform = musicPlayer.GlobalTransform;

        if (radio.repeaterMode) transform.origin += new Vector3(0, radio.depthOfRoom, 0);
        else transform.origin -= new Vector3(0, radio.depthOfRoom, 0);

        musicPlayer.GlobalTransform = transform;
        noisePlayer.GlobalTransform = transform;
    }
}
