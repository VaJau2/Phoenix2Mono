using Godot;
using System.Collections.Generic;

public class RadioControllerTheater : Node
{
    [Export] List<AudioStream> action;
    [Export] AudioStream postAction;
    List<AudioStream> playlist;

    int stage;

    Radio radio;

    public override void _Ready()
    {
        SetProcess(false);

        stage = 0;
        radio = GetNode<Radio>("Radio");
        playlist = radio.GetPlaylist();
    }

    public void OnAction()
    {
        stage = 1;
        radio.SetPlaylist(action);
    }

    public void OnPostAction()
    {
        SetProcess(true);
        stage = 2;
    }

    public void OnMusicFinished()
    {
        if (stage == 2) radio.SetPlaylist(playlist);
    }

    public override void _Process(float delta)
    {
        if (radio.GetVolume() > -80)
        {
            radio.SetVolume(radio.GetVolume() - 0.25f);
        }
        else
        {
            radio.SetPlaylist(postAction);
            radio.SetVolume(0);
            SetProcess(false);
        }
    }
}
