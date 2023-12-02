using Godot;
using Godot.Collections;
using System;

public class Receiver : RadioBase, ISavable, IInteractable
{
	private string model;

	[Export] public bool isOn { protected set; get; } = true;

	public Radiostation station { private set; get; }
	[Export] private Radiostation.Name radiostation;
	[Export] private FrequencyRange frequencyRange;
	[Export] private float frequency = 0.5f;

	[Export] private float unitSize = 10f;
	[Export] private float maxDistance = 120f;

	public enum FrequencyRange
	{
		L,
		M,
		K,
		U1,
		U2
	}

	private Spatial l;
	private Spatial m;
	private Spatial k;
	private Spatial u1;
	private Spatial u2;
	private Spatial arrow;
	private Spatial frequencyLever;
	private Spatial volumeLever;

	private Global global = Global.Get();
	private float minRadioVolume;
	private float maxRadioVolume;

	[Signal]
	public delegate void ChangeMusicEvent();

	[Signal]
	public delegate void ChangeNoiseEvent();

	public bool MayInteract => true;
	public string InteractionHintCode => isOn ? "turnOff" : "turnOn";

	public override void Initialize()
    {
		var settings = GetNode<SettingsSubmenu>("/root/Main/Menu/SettingsMenu/Settings");
		settings.Connect(nameof(SettingsSubmenu.ChangeRadioVolumeEvent), this, nameof(UpdateVolumeLever));
		minRadioVolume = (float)settings.radioSlider.MinValue;
		maxRadioVolume = (float)settings.radioSlider.MaxValue;

		frequency = Mathf.Clamp(frequency, 0, 1);

		InitBase();
		InitModel();
		InitControls();
		InitRadiostation();
		InitPlayer();

		if (inRoom && !radioController.playerInside) RepeaterMode(true);

		if (isOn) SwitchOn(false);
		else SwitchOff(false);
	}

	private void InitModel()
	{
		bool hasRadio = GetNodeOrNull<Spatial>("Radio") != null;
		model = hasRadio ? "Radio" : "Radio Jr";
	}

	private void InitControls()
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
				frequencyLever.Transform = Global.SetNewOrigin(frequencyLever.Transform, new Vector3(0.502f, frequency * 0.526f - 0.258f, -0.488f));

				switch (frequencyRange)
				{
					case FrequencyRange.L:
						l.Transform = Global.SetNewOrigin(l.Transform, new Vector3(0.127f, -0.273f, -0.48f));
						noiseDb = 0;
						break;
					case FrequencyRange.M:
						m.Transform = Global.SetNewOrigin(m.Transform, new Vector3(-0.053f, -0.273f, -0.48f));
						noiseDb = -10;
						break;
					case FrequencyRange.K:
						k.Transform = Global.SetNewOrigin(k.Transform, new Vector3(-0.233f, -0.273f, -0.48f));
						noiseDb = -20;
						break;
					case FrequencyRange.U1:
						u1.Transform = Global.SetNewOrigin(u1.Transform, new Vector3(-0.413f, -0.273f, -0.48f));
						noiseDb = -30;
						break;
					case FrequencyRange.U2:
						u2.Transform = Global.SetNewOrigin(u2.Transform, new Vector3(-0.592f, -0.273f, -0.48f));
						noiseDb = -40;
						break;
				}
				break;

			case "Radio Jr":
				arrow = GetNode<Spatial>("Arrow");
				arrow.Rotate(Vector3.Back, (frequency * 160 + 10) * (float)(Math.PI / 180));

				frequencyLever.Transform = Global.SetNewOrigin(frequencyLever.Transform, new Vector3(-0.58f, frequency * 0.15f + 0.19f, 0f));

				switch (frequencyRange)
				{
					case FrequencyRange.L:
						l.Transform = Global.SetNewOrigin(l.Transform, new Vector3(0.35f, 0.66f, 0f));
						noiseDb = 3;
						break;
					case FrequencyRange.M:
						m.Transform = Global.SetNewOrigin(m.Transform, new Vector3(0.175f, 0.66f, 0f));
						noiseDb = -7;
						break;
					case FrequencyRange.K:
						k.Transform = Global.SetNewOrigin(k.Transform, new Vector3(0f, 0.66f, 0f));
						noiseDb = -17;
						break;
					case FrequencyRange.U1:
						u1.Transform = Global.SetNewOrigin(u1.Transform, new Vector3(-0.175f, 0.66f, 0f));
						noiseDb = -27;
						break;
					case FrequencyRange.U2:
						u2.Transform = Global.SetNewOrigin(u2.Transform, new Vector3(-0.35f, 0.66f, 0f));
						noiseDb = -37;
						break;
				}
				break;
		}
	}

	private void InitRadiostation()
    {
		switch (radiostation)
		{
			case Radiostation.Name.Noise:
				station = null;
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

		station?.Connect(nameof(Radiostation.SyncTimeEvent), this, nameof(SyncTimer));
		station?.Connect(nameof(Radiostation.ChangeSongEvent), this, nameof(OnMusicFinished));
	}

	private void InitPlayer()
	{
		musicPlayer = GetNode<AudioStreamPlayer3D>("Music Player");
		musicPlayer.MaxDistance = maxDistance;
		musicPlayer.UnitSize = unitSize;
		
		noisePlayer = GetNode<AudioStreamPlayer3D>("Noise Player");
		noisePlayer.MaxDistance = maxDistance;
		noisePlayer.UnitSize = unitSize;
		
		switchSound = (AudioStream)GD.Load("res://assets/audio/radio/Switch.ogg");
		noiseSound = (AudioStream)GD.Load("res://assets/audio/radio/Noise.ogg");
	}

	public void Interact(PlayerCamera interactor)
	{
		if (isOn) SwitchOff();
		else SwitchOn();
	}

	public void SwitchOn(bool withSwitchSound = true)
	{
		isOn = true;

		UpdateVolumeLever(global.Settings.radioVolume);

		if (station != null)
        {
			musicPlayer.Stream = station.song;
			station.SyncTimer();
			EmitSignal(nameof(ChangeMusicEvent));
		}

		if (withSwitchSound) PlaySwitchSound();
		else OnSwitchSoundFinished();

		EmitSignal(nameof(ChangeOnline), this);
	}

	public void SwitchOff(bool withSwitchSound = true)
	{
		if (station != null)
        {
			musicPlayer.Stop();
			EmitSignal(nameof(ChangeMusicEvent));
		}

		if (withSwitchSound) PlaySwitchSound();
		UpdateVolumeLever(minRadioVolume);

		isOn = false;
		EmitSignal(nameof(ChangeOnline), this);
	}

	private void SyncTimer()
    {
		if (isOn) musicPlayer.Play(station.timer);
	}

	private void PlaySwitchSound()
    {
		noisePlayer.Stream = switchSound;
		noisePlayer.UnitDb = 0;
		noisePlayer.Play();

		EmitSignal(nameof(ChangeNoiseEvent));
	}

	private void OnSwitchSoundFinished()
	{
		if (isOn)
        {
			noisePlayer.Stream = noiseSound;
			noisePlayer.UnitDb = noiseDb;
			noisePlayer.Play();

			EmitSignal(nameof(ChangeNoiseEvent));
		}
	}

	private void OnMusicFinished()
	{
		if (isOn)
        {
			musicPlayer.Stream = station.song;
			musicPlayer.Play();

			EmitSignal(nameof(ChangeMusicEvent));
		}
	}

	private void UpdateVolumeLever(float value)
    {
		float normalizeLever;
		value = (value - minRadioVolume) / (Mathf.Abs(maxRadioVolume) + Mathf.Abs(minRadioVolume));

		switch (model)
		{
			case "Radio":
				normalizeLever = value * 0.526f - 0.26f;
				volumeLever.Transform = Global.SetNewOrigin(volumeLever.Transform, new Vector3(0.659f, normalizeLever, -0.488f));
				break;

			case "Radio Jr":
				normalizeLever = value * 0.15f + 0.19f;
				volumeLever.Transform = Global.SetNewOrigin(volumeLever.Transform, new Vector3(0.58f, normalizeLever, 0));
				break;
		}
	}

	public Dictionary GetSaveData()
    {
		return new Dictionary()
		{
			{"isOn", isOn}
		};
    }

    public void LoadData(Dictionary data)
    {
		if (!data.Contains("isOn")) return;

		bool tempIsOn = (bool)data["isOn"];
		if (isOn != tempIsOn)
        {
			if (tempIsOn) SwitchOn(false);
			else SwitchOff(false);
		}
	}
}
