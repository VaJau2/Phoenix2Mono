using Godot;
using System;
using System.Collections.Generic;

public class Radiostation : Node
{
	public AudioStream song { private set; get; }

	private Node defaultSongsNode;
	private List<AudioStream> defaultSongs = new ();

	[Export] private bool randomize = true;
	[Export] private List<AudioStream> scriptSongs = new ();
	
	private List<AudioStream> songs = new ();

	public float timer { private set; get; }
	private int songID = 0;

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

	public static Radiostation GetRadiostation(Node node, Name stationName)
	{
		var name = stationName switch
		{
			Name.BlueRadio => "Blue Radio",
			Name.RadioApplewood => "Radio Applewood",
			Name.EasyGreasyFM => "Easy-Greasy FM",
			Name.RoyalRadio => "Royal Radio",
			Name.CountryStation => "Country Station",
			_ => ""
		};
		
		var path = "/root/Main/Scene/Radio Manager/" + name;
		return node.GetNodeOrNull<Radiostation>(path);
	}
}
