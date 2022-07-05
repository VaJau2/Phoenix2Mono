using Godot;
using System.Collections.Generic;

public class Playlist : Node
{
	[Export] public bool isAction = false;
	List<AudioStream> playlist = new List<AudioStream>();

	int musicID = 0;
	AudioStreamPlayer3D musicPlayer;

	public override void _Ready()
	{
		musicPlayer = GetNode<AudioStreamPlayer3D>("Music Player");
	}

	public void OnMusicFinished()
    {
		if (musicID < playlist.Count - 1) musicID++;
		else musicID = 0;

		musicPlayer.Stream = playlist[musicID];
		musicPlayer.Play();
    }

	public void Update(List<AudioStream> newPlaylist)
    {
		playlist = newPlaylist;

		musicPlayer = GetNodeOrNull<AudioStreamPlayer3D>("Music Player");
		musicPlayer.Play(0);
	}
}
