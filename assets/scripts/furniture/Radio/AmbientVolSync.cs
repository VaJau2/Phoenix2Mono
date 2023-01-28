using Godot;
using System;
using System.Collections.Generic;

public class AmbientVolSync : Spatial
{
    float distance;
    bool isEmpty = true;

    List<RadioBase> radioList = new List<RadioBase>();
    RadioBase nearestRadio;

    Global global = Global.Get();
    float minAmbientVolume;

    public override void _Ready()
    {
        var settings = GetNode<SettingsSubmenu>("/root/Main/Menu/SettingsMenu/Settings");
        minAmbientVolume = (float)settings.radioSlider.MinValue;

        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        distance = 100;
        isEmpty = true;

        foreach (RadioBase radio in radioList)
        {
            if (radio.musicPlayer.Playing || radio.noisePlayer.Playing)
            {
                isEmpty = false;
                float newDistance = GlobalTransform.origin.DistanceTo(radio.GlobalTransform.origin);

                if (newDistance <= distance)
                {
                    distance = newDistance;
                    nearestRadio = radio;
                }
            }
        }

        if (isEmpty)
        {
            global.Settings.UpdateAudioBus(AudioBus.Music, global.Settings.musicVolume);
            SetProcess(false);
        }
        else
        {
            float distanceRatio = distance / 100;
            float volume = minAmbientVolume + (distanceRatio * (global.Settings.musicVolume + Mathf.Abs(minAmbientVolume)));
            global.Settings.UpdateAudioBus(AudioBus.Music, volume);
        }
    }

    public void _on_body_entered(Node body)
    {
        if (!(body is RadioBase radio)) return;

        radioList.Add(radio);
        radio.Connect(nameof(RadioBase.ChangeOnline), this, nameof(OnRadioChangeOnline));
        if (!IsProcessing()) SetProcess(true);
    }

    void _on_body_exited(Node body)
    {
        if (!(body is RadioBase radio)) return;
        
        if (radioList.Contains(radio))
        {
            radio.Disconnect(nameof(RadioBase.ChangeOnline), this, nameof(OnRadioChangeOnline));
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
        if (isEmpty && radio.musicPlayer.Playing)
        {
            SetProcess(true);
        }
    }
}
