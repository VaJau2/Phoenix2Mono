using Godot;
using System;
using System.Collections.Generic;

public partial class Radiostation : Node
{
	public AudioStream song { private set; get; }

	private Node defaultSongsNode;
	private List<AudioStream> defaultSongs = new ();

	[Export] private bool randomize = true;
	[Export] private Godot.Collections.Array<AudioStream> scriptSongs = new ();
	
	private List<AudioStream> songs = new ();

	public float timer { private set; get; }
	private int songID = 0;

	[Signal]
	public delegate void SyncTimeEventEventHandler();

	[Signal]
	public delegate void ChangeSongEventEventHandler(AudioStream song);

	public new enum Name
	{
		Noise,
		RadioApplewood,
		EasyGreasyFM,
		RoyalRadio,
		BlueRadio,
		CountryStation
	}

	// Синхронизация плееров (B-42): Godot 4 перематывает MP3 медленно и по очереди, плееры расходятся.
	// Ушедшие вперёд ставим на паузу, пока их не догонит отстающий.
	private const float SYNC_TOLERANCE = 0.02f; // допустимое расхождение, с
	private const int SYNC_WAIT_FRAMES = 3;     // кадров до замера (старт — в physics process)
	private const int SYNC_MAX_ROUNDS = 10;

	private readonly List<AudioStreamPlayer3D> players = [];     // плееры песни станции
	private readonly List<AudioStreamPlayer3D> heldPlayers = []; // на паузе для выравнивания
	private AudioStreamPlayer3D syncReference;                   // отстающий плеер
	private int syncWaitFrames = -1;
	private int syncRounds;

	public override void _Process(double delta)
    {
		timer += (float)delta;
		if (timer >= song.GetLength())
        {
			timer = 0;
			OnMusicFinished();
		}

		SyncPlayers(delta);
    }

	// Вместо player.Play(from) для песни станции
	public void PlaySong(AudioStreamPlayer3D player, float from)
	{
		heldPlayers.Remove(player);
		player.StreamPaused = false;
		player.Play(from);

		if (!players.Contains(player)) players.Add(player);
		syncWaitFrames = SYNC_WAIT_FRAMES;
		syncRounds = 0;
	}

	// Позиция с учётом времени с последнего микширования
	private static float GetPosition(AudioStreamPlayer3D player)
	{
		var position = player.GetPlaybackPosition();
		return player.StreamPaused ? position : position + (float)AudioServer.GetTimeSinceLastMix();
	}

	private void SyncPlayers(double delta)
	{
		// убираем остановленные и сменившие звук; на паузе Playing == false — их оставляем
		players.RemoveAll(p => !IsInstanceValid(p) || p.Stream != song || (!p.Playing && !heldPlayers.Contains(p)));
		heldPlayers.RemoveAll(p =>
		{
			if (players.Contains(p)) return false;
			if (IsInstanceValid(p)) p.StreamPaused = false;
			return true;
		});

		if (heldPlayers.Count > 0)
		{
			if (!players.Contains(syncReference))
			{
				foreach (var held in heldPlayers) held.StreamPaused = false;
				heldPlayers.Clear();
				syncWaitFrames = SYNC_WAIT_FRAMES;
				return;
			}

			// снимаем с паузы, когда отстающий дошёл до позиции плеера
			var referencePosition = GetPosition(syncReference) + (float)delta / 2;
			heldPlayers.RemoveAll(p =>
			{
				if (referencePosition < p.GetPlaybackPosition()) return false;
				p.StreamPaused = false;
				return true;
			});

			// после выравнивания — повторный замер
			if (heldPlayers.Count == 0) syncWaitFrames = SYNC_WAIT_FRAMES;
			return;
		}

		if (syncWaitFrames < 0 || syncWaitFrames-- > 0) return;
		if (players.Count == 0) return;

		var positions = new Dictionary<AudioStreamPlayer3D, float>();
		syncReference = players[0];
		foreach (var player in players)
		{
			positions[player] = GetPosition(player);
			if (positions[player] < positions[syncReference]) syncReference = player;
		}

		var minPosition = positions[syncReference];
		foreach (var player in players)
		{
			if (positions[player] - minPosition > SYNC_TOLERANCE) heldPlayers.Add(player);
		}

		if (heldPlayers.Count == 0 || ++syncRounds > SYNC_MAX_ROUNDS)
		{
			heldPlayers.Clear();
			syncWaitFrames = -1;
			// таймер — по реальной позиции музыки
			timer = minPosition;
			return;
		}

		foreach (var held in heldPlayers) held.StreamPaused = true;
	}

	public void Initialize()
    {
		defaultSongsNode = GetNode("Default Songs");
		var defaultSongsArray = defaultSongsNode.Get("songs").AsArrayOrNull();
		
		foreach (AudioStream defaultSong in defaultSongsArray)
        {
			defaultSongs.Add(defaultSong);
        }

		if (randomize)
        {
			defaultSongs = Randomize(defaultSongs);
			if (scriptSongs != null) scriptSongs = new Godot.Collections.Array<AudioStream>(Randomize(scriptSongs));
		}

		if (scriptSongs != null) songs.AddRange(scriptSongs);
		songs.AddRange(defaultSongs);

		song = songs[0];

		var random = new Random();
		timer = random.Next(0, (int)song.GetLength());
	}

	private List<AudioStream> Randomize(IList<AudioStream> playlist)
	{
		var newPlaylist = new List<AudioStream>();
		var random = new Random();

		while (playlist.Count > 0)
		{
			var index = random.Next(0, playlist.Count);
			newPlaylist.Add(playlist[index]);
			playlist.RemoveAt(index);
		}

		return newPlaylist;
	}

	private void OnMusicFinished()
	{
		if (songID < songs.Count - 1) songID++;
		else songID = 0;

		song = songs[songID];
		EmitSignal(SignalName.ChangeSongEvent, song);
	}
	
	public void SyncTimer()
    {
		EmitSignal(SignalName.SyncTimeEvent);
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
