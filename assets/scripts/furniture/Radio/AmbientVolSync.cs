using Godot;
using System.Collections.Generic;

public partial class AmbientVolSync : Node3D
{
    float distance;
    bool isEmpty = true;

    List<RadioBase> radioList = new();
    RadioBase nearestRadio;

    Global global = Global.Get();
    float minVolume;

    public override void _Ready()
    {
        var settings = GetNode<SettingsSubmenu>("/root/Main/Menu/SettingsMenu/Settings");
        settings.ChangeRadioVolume += OnChangeRadioVolume;
        minVolume = (float)settings.musicSlider.MinValue;

        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        distance = 100;
        isEmpty = true;

        foreach (RadioBase radio in radioList)
        {
            if (radio.MusicPlayer.Playing || radio.NoisePlayer.Playing)
            {
                isEmpty = false;
                float newDistance = GlobalTransform.Origin.DistanceTo(radio.GlobalTransform.Origin);

                if (newDistance <= distance)
                {
                    distance = newDistance;
                    nearestRadio = radio;
                }
            }
        }

        if (isEmpty || global.Settings.radioVolume == minVolume)
        {
            global.Settings.UpdateAudioBus(AudioBus.Music, global.Settings.musicVolume);
            SetProcess(false);
        }
        else
        {
            float distanceRatio = distance / 100;
            float volume = minVolume + (distanceRatio * (global.Settings.musicVolume + Mathf.Abs(minVolume)));
            global.Settings.UpdateAudioBus(AudioBus.Music, volume);
        }
    }

    public void _on_body_entered(Node body)
    {
        if (!(body is RadioBase radio)) return;

        radioList.Add(radio);
        
        if (radio.IsConnected(RadioBase.SignalName.ChangeOnline, new Callable(this, nameof(OnRadioChangeOnline)))) 
            return;
        
        radio.ChangeOnline += OnRadioChangeOnline;
        
        if (!IsProcessing() && global.Settings.radioVolume > minVolume)
        {
            SetProcess(true);
        }
    }

    void _on_body_exited(Node body)
    {
        if (!(body is RadioBase radio)) return;
        
        if (radioList.Contains(radio))
        {
            radio.ChangeOnline -= OnRadioChangeOnline;
            radioList.Remove(radio);

            if (radioList.Count == 0)
            {
                SetProcess(false);
                global.Settings.UpdateAudioBus(AudioBus.Music, global.Settings.musicVolume);
            }
        }
    }

    public void Clear()
    {
        radioList.Clear();
        SetProcess(false);
    }

    void OnRadioChangeOnline(RadioBase radio)
    {
        if (isEmpty && radio.MusicPlayer.Playing && global.Settings.radioVolume > minVolume)
        {
            SetProcess(true);
        }
    }

    public void OnChangeRadioVolume(float value)
    {
        if (!isEmpty && value > minVolume)
        {
            SetProcess(true);
        }
    }
}
