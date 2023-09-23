using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public class Radiostation : Node
{
	public AudioStream song { private set; get; }

	Node defaultSongsNode;
	List<AudioStream> defaultSongs = new List<AudioStream>();

	[Export] bool randomize = true;
	[Export] List<AudioStream> scriptSongs = new List<AudioStream>();
	
	List<AudioStream> songs = new List<AudioStream>();

	public float timer { private set; get; }
	int songID = 0;

	[Signal]
	public delegate void SyncTimeEvent();

	[Signal]
	public delegate void ChangeSongEvent();

	public new enum Name
	{
		Noise,
		RadioApplewood,
		EasyGreasyFM,
		RoyalRadio,
		BlueRadio,
		CountryStation
	}

	public override void _Process(float delta)
    {
		timer += delta;
		if (timer >= song.GetLength())
        {
			timer = 0;
			OnMusicFinished();
		}
    }

	public void Initialize()
    {
		defaultSongsNode = GetNode("Default Songs");
		Godot.Collections.Array defaultSongsArray = defaultSongsNode.Get("songs") as Godot.Collections.Array;
		
		foreach (AudioStream defaultSong in defaultSongsArray)
        {
			defaultSongs.Add(defaultSong);
        }

		if (randomize)
        {
			defaultSongs = Randomize(defaultSongs);
			if (scriptSongs != null) scriptSongs = Randomize(scriptSongs);
		}

		if (scriptSongs != null) songs.AddRange(scriptSongs);
		songs.AddRange(defaultSongs);

		song = songs[0];

		Random random = new Random();
		timer = random.Next(0, (int)song.GetLength());
	}

	public List<AudioStream> Randomize(List<AudioStream> playlist)
	{
		List<AudioStream> newPlaylist = new List<AudioStream>();
		Random random = new Random();
		int index;

		while (playlist.Count > 0)
		{
			index = random.Next(0, playlist.Count);
			newPlaylist.Add(playlist[index]);
			playlist.RemoveAt(index);
		}

		return newPlaylist;
	}

	public void SyncTimer()
    {
		EmitSignal(nameof(SyncTimeEvent));
    }

	public void OnMusicFinished()
	{
		if (songID < songs.Count - 1) songID++;
		else songID = 0;

		song = songs[songID];
		EmitSignal(nameof(ChangeSongEvent));
	}

	public static string GetRadiostation(Name stationName)
    {
		switch (stationName)
		{
			case Name.Noise:
				return "/root/Main/Scene/RadioController/Noise";

			case Name.RadioApplewood:
				return "/root/Main/Scene/RadioController/Radio Applewood";

			case Name.EasyGreasyFM:
				return "/root/Main/Scene/RadioController/Easy-Greasy FM";

			case Name.RoyalRadio:
				return "/root/Main/Scene/RadioController/Royal Radio";

			case Name.BlueRadio:
				return "/root/Main/Scene/RadioController/Blue Radio";

			case Name.CountryStation:
				return "/root/Main/Scene/RadioController/Country Station";

			default:
				return "Invalid Radiostation Name";
		}
	}
}
