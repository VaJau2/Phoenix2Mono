using Godot;
using Godot.Collections;
using System;

public class Radio : StaticBody, ISavable
{
	string model;

	[Export] public bool isOn { private set; get; } = true;
	public bool inRoom = false;
	bool repeaterMode = false;

	[Export] Radiostation.Name radiostation;
	Radiostation station;
	[Export] FrequencyRange frequencyRange;
	[Export] float frequency = 0.5f;
	
	float noiseDb;

	bool saveSettingsLoaded = false;

	public enum FrequencyRange
	{
		L,
		M,
		K,
		U1,
		U2
	}

	AudioStreamPlayer3D musicPlayer;
	AudioStreamPlayer3D noisePlayer;
	AudioStream noiseSound;
	AudioStream switchSound;

	Spatial volumeLever;
	Spatial frequencyLever;
	Spatial arrow;
	Spatial l;
	Spatial m;
	Spatial k;
	Spatial u1;
	Spatial u2;

	RadioController radioController;

	float depthOfRoom = 100 / 1.5f;

	public void Initialize()
	{
		frequency = Mathf.Clamp(frequency, 0, 1);
		Init();
	}

	public void Initialize(Radiostation.Name _radiostation, FrequencyRange _frequencyRange, float _frequency)
	{
		radiostation = _radiostation;
		frequencyRange = _frequencyRange;
		frequency = Mathf.Clamp(_frequency, 0, 1);
		Init();
	}

	void Init()
    {
		InitModel();
		InitControls();
		InitRadiostation();
		InitPlayer();

		radioController = GetNode<RadioController>("/root/Main/Scene/RadioController");

		if (saveSettingsLoaded) return;

		if (inRoom && !radioController.playerInside) RepeaterMode(true);
				
		if (isOn) SwitchOn(false);
		else SwitchOff(false);
	}

	void InitModel()
	{
		bool hasRadio = GetNodeOrNull<Spatial>("Radio") != null;
		model = hasRadio ? "Radio" : "Radio Jr";

		if (model == "Radio Jr")
		{
			arrow = GetNode<Spatial>("Arrow");
		}
	}

	void InitControls()
    {
		volumeLever = GetNode<Spatial>("Volume Lever");
		frequencyLever = GetNode<Spatial>("Frequency Lever");

		l = GetNode<Spatial>("L");
		m = GetNode<Spatial>("M");
		k = GetNode<Spatial>("K");
		u1 = GetNode<Spatial>("U1");
		u2 = GetNode<Spatial>("U2");

		switch (model)
		{
			case "Radio":
				frequencyLever.Transform = Global.setNewOrigin(frequencyLever.Transform, new Vector3(0.502f, frequency * 0.526f - 0.258f, -0.488f));

				switch (frequencyRange)
				{
					case FrequencyRange.L:
						l.Transform = Global.setNewOrigin(l.Transform, new Vector3(0.127f, -0.273f, -0.48f));
						noiseDb = 0;
						break;
					case FrequencyRange.M:
						m.Transform = Global.setNewOrigin(m.Transform, new Vector3(-0.053f, -0.273f, -0.48f));
						noiseDb = -10;
						break;
					case FrequencyRange.K:
						k.Transform = Global.setNewOrigin(k.Transform, new Vector3(-0.233f, -0.273f, -0.48f));
						noiseDb = -20;
						break;
					case FrequencyRange.U1:
						u1.Transform = Global.setNewOrigin(u1.Transform, new Vector3(-0.413f, -0.273f, -0.48f));
						noiseDb = -30;
						break;
					case FrequencyRange.U2:
						u2.Transform = Global.setNewOrigin(u2.Transform, new Vector3(-0.592f, -0.273f, -0.48f));
						noiseDb = -40;
						break;
				}
				break;

			case "Radio Jr":
				frequencyLever.Transform = Global.setNewOrigin(frequencyLever.Transform, new Vector3(-0.58f, frequency * 0.15f + 0.19f, 0f));
				arrow.Rotate(Vector3.Back, (frequency * 160 + 10) * (float)(Math.PI / 180));

				switch (frequencyRange)
				{
					case FrequencyRange.L:
						l.Transform = Global.setNewOrigin(l.Transform, new Vector3(0.35f, 0.66f, 0f));
						noiseDb = 3;
						break;
					case FrequencyRange.M:
						m.Transform = Global.setNewOrigin(m.Transform, new Vector3(0.175f, 0.66f, 0f));
						noiseDb = -7;
						break;
					case FrequencyRange.K:
						k.Transform = Global.setNewOrigin(k.Transform, new Vector3(0f, 0.66f, 0f));
						noiseDb = -17;
						break;
					case FrequencyRange.U1:
						u1.Transform = Global.setNewOrigin(u1.Transform, new Vector3(-0.175f, 0.66f, 0f));
						noiseDb = -27;
						break;
					case FrequencyRange.U2:
						u2.Transform = Global.setNewOrigin(u2.Transform, new Vector3(-0.35f, 0.66f, 0f));
						noiseDb = -37;
						break;
				}
				break;
		}
	}

	void InitRadiostation()
    {
		switch (radiostation)
		{
			case Radiostation.Name.Noise:
				station = GetNode<Radiostation>("/root/Main/Scene/RadioController/Noise");
				break;

			case Radiostation.Name.RadioApplewood:
				station = GetNode<Radiostation>("/root/Main/Scene/RadioController/Radio Applewood");
				break;

			case Radiostation.Name.EasyGreasyFM:
				station = GetNode<Radiostation>("/root/Main/Scene/RadioController/Easy-Greasy FM");
				break;

			case Radiostation.Name.RoyalRadio:
				station = GetNode<Radiostation>("/root/Main/Scene/RadioController/Royal Radio");
				break;

			case Radiostation.Name.BlueRadio:
				station = GetNode<Radiostation>("/root/Main/Scene/RadioController/Blue Radio");
				break;

			case Radiostation.Name.CountryStation:
				station = GetNode<Radiostation>("/root/Main/Scene/RadioController/Country Station");
				break;

		}
	}

	void InitPlayer()
	{
		Global.OnPauseChange += OnPauseChange;
		station.OnChangeSong += OnMusicFinished;

		noisePlayer = GetNode<AudioStreamPlayer3D>("Noise Player");
		switchSound = (AudioStream)GD.Load("res://assets/audio/radio/Switch.ogg");
		noiseSound = (AudioStream)GD.Load("res://assets/audio/radio/Noise.ogg");

		musicPlayer = GetNode<AudioStreamPlayer3D>("Music Player");
	}

    public override void _ExitTree()
	{
		Global.OnPauseChange -= OnPauseChange;
		station.OnChangeSong -= OnMusicFinished;
	}

	void OnMusicFinished()
	{
		musicPlayer.Stream = station.song;
		musicPlayer.Play();
	}

	public void OnSwitchSoundFinished()
    {
		if (isOn && noisePlayer.Stream != noiseSound)
        {
			noisePlayer.Stream = noiseSound;
			noisePlayer.UnitDb = noiseDb;
			noisePlayer.Play();
		}
    }

	void OnPauseChange()
	{
		musicPlayer.MaxDb = Global.Get().paused ? -24 : 0;
		noisePlayer.MaxDb = Global.Get().paused ? -24 : 0;
	}

	public void Interactive()
	{
		if (isOn) SwitchOff();
		else SwitchOn();
	}

	void SwitchOn(bool withSwitchSound = true)
	{
		isOn = true;

		switch (model)
		{
			case "Radio":
				volumeLever.Transform = Global.setNewOrigin(volumeLever.Transform, new Vector3(0.659f, 0.268f, -0.488f));
				break;

			case "Radio Jr":
				volumeLever.Transform = Global.setNewOrigin(volumeLever.Transform, new Vector3(0.58f, 0.34f, 0));
				break;
		}

		if (withSwitchSound)
        {
			noisePlayer.Stream = switchSound;
			noisePlayer.UnitDb = 0;
			noisePlayer.Play();
		}
		else OnSwitchSoundFinished();

		musicPlayer.Stream = station.song;
		musicPlayer.UnitDb = 0;
		musicPlayer.Play(station.timer);
	}

	void SwitchOff(bool withSwitchSound = true)
	{
		if (withSwitchSound)
		{
			noisePlayer.Stream = switchSound;
			noisePlayer.UnitDb = 0;
			noisePlayer.Play();
		}

		musicPlayer.UnitDb = -80;

		switch (model)
		{
			case "Radio":
				volumeLever.Transform = Global.setNewOrigin(volumeLever.Transform, new Vector3(0.659f, -0.258f, -0.488f));
				break;

			case "Radio Jr":
				volumeLever.Transform = Global.setNewOrigin(volumeLever.Transform, new Vector3(0.58f, 0.19f, 0));
				break;
		}

		isOn = false;
	}

	public void SetMute(bool value)
	{
		if (value)
        {
			musicPlayer.UnitDb = -80;
			noisePlayer.UnitDb = -80;
		}
		else
        {
			musicPlayer.UnitDb = 0;
			noisePlayer.UnitDb = noiseDb;
		}
	}

	public void RepeaterMode(bool value)
    {
		var transform = musicPlayer.GlobalTransform;

		if (value) transform.origin += new Vector3(0, depthOfRoom, 0);
		else transform.origin -= new Vector3(0, depthOfRoom, 0);

		musicPlayer.GlobalTransform = transform;
		repeaterMode = value;
	}

    public Dictionary GetSaveData()
    {
		return new Dictionary()
		{
			{"isOn", isOn},
			{"repeat_mode", repeaterMode}
		};
    }

    public void LoadData(Dictionary data)
    {
		if (!data.Contains("isOn")) return;

		bool tempIsOn = (bool)data["isOn"];
		if (tempIsOn) SwitchOn(false);
		else SwitchOff(false);
		
		RepeaterMode((bool)data["repeat_mode"]);

		saveSettingsLoaded = true;
	}
}
