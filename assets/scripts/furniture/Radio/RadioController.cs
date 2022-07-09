using Godot;
using System.Collections.Generic;

public class RadioController : Node
{
    [Export] List<NodePath> radioPaths = new List<NodePath>();
    List<Radio> radios = new List<Radio>();
    List<AudioStream> playlist; 

    public override void _Ready()
    {
        foreach (NodePath tempPath in radioPaths)
        {
            radios.Add(GetNode<Radio>(tempPath));
        }

        playlist = radios[0].GetPlaylist();
        foreach (Radio radio in radios)
        {
            radio.SetPlaylist(playlist);
        }

        SetProcess(false);
    }
}
