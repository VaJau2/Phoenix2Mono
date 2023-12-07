using Godot;
using Godot.Collections;
using System;

public abstract class RadioBase : StaticBody
{
    public bool inRoom = false;
    public RadioManager RadioManager;
    
    public AudioStreamPlayer3D musicPlayer { protected set; get; }

    public AudioStreamPlayer3D noisePlayer { protected set; get; }
    protected AudioStream noiseSound;
    protected AudioStream switchSound;
    protected float noiseDb = -40;

    [Export] float maxDb = -24;

    public bool repeaterMode { protected set; get; } = false;
    public float depthOfRoom = 100 / 1.5f;

    [Signal]
    public delegate void ChangeOnline(RadioBase radio);

    public abstract void Initialize();

    protected void InitBase()
    { 
        musicPlayer = GetNode<AudioStreamPlayer3D>("Music Player");
        
        noisePlayer = GetNode<AudioStreamPlayer3D>("Noise Player");
        switchSound = (AudioStream)GD.Load("res://assets/audio/radio/Switch.ogg");
        noiseSound = (AudioStream)GD.Load("res://assets/audio/radio/Noise.ogg");

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
            musicPlayer.UnitDb = -80;
            noisePlayer.UnitDb = -80;
        }
        else
        {
            musicPlayer.UnitDb = 0;
            noisePlayer.UnitDb = noiseDb;
        }
    }

    protected void OnPauseChange(bool value)
    {
        musicPlayer.MaxDb = value ? maxDb : 0;
        noisePlayer.MaxDb = value ? maxDb : 0;
    }
}
