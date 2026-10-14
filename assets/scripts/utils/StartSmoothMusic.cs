using Godot;
using Godot.Collections;

//Музыка стартует плавно в начале уровня
//А если загружается сохранение, то играет сразу с полной громкости
public class StartSmoothMusic : AudioStreamPlayer, ISavable
{
    [Export] private float startDelayTimer = 2f;
    [Export] private float riseSpeed = 0.1f;
    [Export] private float startVolume = -20f;
    [Export] private float maxVolume = 2f;
    [Export] private bool loadLoudMusic;

    public Dictionary GetSaveData()
    {
        return null;
    }

    public void LoadData(Dictionary data)
    {
        if (!loadLoudMusic) return;
        if (!Playing)
        {
            Play();
        }
        VolumeDb = maxVolume;
        SetProcess(false);
    }

    public override void _Ready()
    {
        VolumeDb = startVolume;
    }

    public override void _Process(float delta)
    {
        if (startDelayTimer > 0)
        {
            startDelayTimer -= delta;
            return;
        }

        if (!Playing)
        {
            Play();
        }

        if (VolumeDb < maxVolume)
        {
            VolumeDb += riseSpeed;
            return;
        }

        SetProcess(false);
    }
}
