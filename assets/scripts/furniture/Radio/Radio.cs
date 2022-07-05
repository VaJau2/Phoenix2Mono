using Godot;
using System;
using System.Collections.Generic;

public class Radio : StaticBody
{
	string model;
	[Export] public bool isOn = true;
	[Export] float frequency = 0.5f;

	enum FrequencyRange
    {
		L,
		M,
		K,
		U1,
		U2
    }
	[Export] FrequencyRange frequencyRange;

	[Export] bool isNoise = false;
	[Export] bool randomize = true;
	[Export] List<AudioStream> playlist;

	int musicID = 0;
	AudioStreamPlayer3D musicPlayer;

	AudioStreamPlayer3D noise;
	AudioStream switchSound;
	AudioStream noiseSound;

	Spatial volumeLever;
	Spatial frequencyLever;
	Spatial arrow;
	Spatial l;
	Spatial m;
	Spatial k;
	Spatial u1;
	Spatial u2;

	public override void _Ready()
	{
		Global.OnPauseChange += OnPauseChange;

		if (GetNodeOrNull<Spatial>("Radio") != null) model = "Radio";
		else model = "Radio Jr";

		noise = GetNode<AudioStreamPlayer3D>("Noise");
		switchSound = (AudioStream)GD.Load("res://assets/audio/radio/Switch.ogg");
		noiseSound = (AudioStream)GD.Load("res://assets/audio/radio/Noise.ogg");

		musicPlayer = GetNode<AudioStreamPlayer3D>("Music Player");

		volumeLever = GetNode<Spatial>("Volume Lever");
		frequencyLever = GetNode<Spatial>("Frequency Lever");
		if (model == "Radio Jr") arrow = GetNode<Spatial>("Arrow");
		l = GetNode<Spatial>("L");
		m = GetNode<Spatial>("M");
		k = GetNode<Spatial>("K");
		u1 = GetNode<Spatial>("U1");
		u2 = GetNode<Spatial>("U2");

		if (frequency >= 1) frequency = 1;
		else if (frequency >= 0 && frequency < 1) frequency -= (int)frequency;
		else frequency = 0;

		switch (model)
        {
			case "Radio":
				MoveTo(frequencyLever, new Vector3(0.502f, frequency * 0.526f - 0.258f, -0.488f));

				switch (frequencyRange)
				{
					case FrequencyRange.L:
						MoveTo(l, new Vector3(0.127f, -0.273f, -0.48f));
						break;
					case FrequencyRange.M:
						MoveTo(m, new Vector3(-0.053f, -0.273f, -0.48f));
						break;
					case FrequencyRange.K:
						MoveTo(k, new Vector3(-0.233f, -0.273f, -0.48f));
						break;
					case FrequencyRange.U1:
						MoveTo(u1, new Vector3(-0.413f, -0.273f, -0.48f));
						break;
					case FrequencyRange.U2:
						MoveTo(u2, new Vector3(-0.592f, -0.273f, -0.48f));
						break;
				}
				break;

			case "Radio Jr":
				MoveTo(frequencyLever, new Vector3(-0.58f, frequency * 0.15f + 0.19f, 0f));
				arrow.Rotate(Vector3.Back, (frequency * 160 + 10) * (float)(Math.PI / 180));

				switch (frequencyRange)
				{
					case FrequencyRange.L:
						MoveTo(l, new Vector3(0.35f, 0.66f, 0f));
						break;
					case FrequencyRange.M:
						MoveTo(m, new Vector3(0.175f, 0.66f, 0f));
						break;
					case FrequencyRange.K:
						MoveTo(k, new Vector3(0f, 0.66f, 0f));
						break;
					case FrequencyRange.U1:
						MoveTo(u1, new Vector3(-0.175f, 0.66f, 0f));
						break;
					case FrequencyRange.U2:
						MoveTo(u2, new Vector3(-0.35f, 0.66f, 0f));
						break;
				}
				break;
        }

		if (isOn)
        {
			musicPlayer.UnitDb = 0;

			switch (model)
            {
				case "Radio":
					MoveTo(volumeLever, new Vector3(0.659f, 0.268f, -0.488f));
					break;

				case "Radio Jr":
					MoveTo(volumeLever, new Vector3(0.58f, 0.34f, 0));
					break;
			}
		}
		else
        {
			musicPlayer.UnitDb = -80;

			switch (model)
			{
				case "Radio":
					MoveTo(volumeLever, new Vector3(0.659f, -0.258f, -0.488f));
					break;

				case "Radio Jr":
					MoveTo(volumeLever, new Vector3(0.58f, 0.19f, 0));
					break;
			}
		}

		if (isNoise)
		{
			noise.Stream = noiseSound;
			noise.Play();
		}
		else if (playlist.Count != 0)
		{
			if (randomize) Randomize();
			else
			{
				musicPlayer.Stream = playlist[musicID];
				musicPlayer.Play();
			}
		}
	}

	public void OnMusicFinished()
	{
		if (musicID < playlist.Count - 1) musicID++;
		else musicID = 0;

		musicPlayer.Stream = playlist[musicID];
		musicPlayer.Play();
	}

	public void OnNoiseFinished()
    {
		if (isOn && isNoise)
        {
			noise.Stream = noiseSound;
			noise.Play();
		}
    }

	void OnPauseChange()
    {
		musicPlayer.MaxDb = Global.Get().paused ? -24 : 3;
		noise.MaxDb = Global.Get().paused ? -24 : 3;
	}

	public float GetVolume()
    {
		return musicPlayer.MaxDb;
    }

	public void SetVolume(float volume)
    {
		musicPlayer.MaxDb = volume;
    }

	public List<AudioStream> GetPlaylist()
    {
		return playlist;
    }

	public void SetPlaylist(AudioStream music)
	{
		playlist.Clear();
		playlist.Add(music);
		musicPlayer.Stream = playlist[0];
		musicPlayer.Play();
	}

	public void SetPlaylist(List<AudioStream> newPlaylist)
	{
		playlist = newPlaylist;
		musicPlayer.Stream = playlist[0];
		musicPlayer.Play();
	}

	public void Randomize()
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

		playlist = newPlaylist;
		musicPlayer.Stream = playlist[musicID];
		musicPlayer.Play(random.Next(0, (int)musicPlayer.Stream.GetLength()));
	}

	public void Interactive()
    {
		if (isOn) SwitchOff();
		else SwitchOn();
    }

	public void SwitchOn()
    {
		isOn = true;

		switch (model)
		{
			case "Radio":
				MoveTo(volumeLever, new Vector3(0.659f, 0.268f, -0.488f));
				break;

			case "Radio Jr":
				MoveTo(volumeLever, new Vector3(0.58f, 0.34f, 0));
				break;
		}

		noise.Stream = switchSound;
		noise.Play();

		musicPlayer.UnitDb = 0;
	}

	public void SwitchOff()
    {
		musicPlayer.UnitDb = -80;

		noise.Stream = switchSound;
		noise.Play();

		switch (model)
		{
			case "Radio":
				MoveTo(volumeLever, new Vector3(0.659f, -0.258f, -0.488f));
				break;

			case "Radio Jr":
				MoveTo(volumeLever, new Vector3(0.58f, 0.19f, 0));
				break;
		}

		isOn = false;
	}

	void MoveTo(Spatial obj, Vector3 target)
    {
		Transform trans = obj.Transform;
		trans.origin = target;
		obj.Transform = trans;
	}
}
