using Godot;
using Godot.Collections;
using System;

public abstract class RadioBase : StaticBody
{
    public bool InRoom = false;
    public RadioManager RadioManager;
    protected WarningManager warningManager;
    
    [Export] protected float unitSize = 10f;
    [Export] protected float maxDistance = 120f;
    public AudioStreamPlayer3D MusicPlayer { protected set; get; }
    public AudioStreamPlayer3D NoisePlayer { protected set; get; }
    protected AudioStream noiseSound;
    protected AudioStream switchSound;
    protected float noiseDb = -40;

    [Export] private float pauseVolume = -24;

    public bool repeaterMode { protected set; get; } = false;
    public float depthOfRoom = 100 / 1.5f;
    
    [Signal]
    public delegate void ChangeOnline(RadioBase radio);

    public abstract void Initialize();

    protected void InitBase()
    {
        MusicPlayer = GetNode<AudioStreamPlayer3D>("Music Player");
        MusicPlayer.MaxDistance = maxDistance;
        MusicPlayer.UnitSize = unitSize;
        
        NoisePlayer = GetNode<AudioStreamPlayer3D>("Noise Player");
        NoisePlayer.MaxDistance = maxDistance;
        NoisePlayer.UnitSize = unitSize;
        
        switchSound = (AudioStream)GD.Load("res://assets/audio/radio/Switch.ogg");
        noiseSound = (AudioStream)GD.Load("res://assets/audio/radio/Noise.ogg");

        warningManager = GetNodeOrNull<WarningManager>("/root/Main/Scene/Warning Manager");
        
        var pauseMenu = GetNode<PauseMenu>("/root/Main/Menu/PauseMenu");
        pauseMenu.Connect(nameof(PauseMenu.ChangePause), this, nameof(OnPauseChange));
    }

    public void RepeaterMode(bool value)
    {
        var transform = GlobalTransform;

        if (value) transform.origin += new Vector3(0, depthOfRoom, 0);
        else transform.origin -= new Vector3(0, depthOfRoom, 0);

        GlobalTransform = transform;
        repeaterMode = value;
    }

    public void SetMute(bool value)
    {
        if (value)
        {
            MusicPlayer.UnitDb = -80;
            NoisePlayer.UnitDb = -80;
        }
        else
        {
            MusicPlayer.UnitDb = 0;
            NoisePlayer.UnitDb = noiseDb;
        }
    }

    protected void OnPauseChange(bool value)
    {
        MusicPlayer.MaxDb = value ? pauseVolume : 0;
        NoisePlayer.MaxDb = value ? pauseVolume : 0;
    }
}
