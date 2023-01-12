using Godot;
using System;
using System.Collections.Generic;

public class RadioController : Node
{
    [Export] public bool playerInside;

    [Export] bool monoStation = true;
    [Export] Radiostation.Name radiostation;
    [Export] float frequency;
    [Export] Radio.FrequencyRange frequencyRange;

    [Export] List<NodePath> radioListPath = new List<NodePath>();
    List<Radio> radioList = new List<Radio>();

    public override void _Ready()
    {
        var stations = GetChildren();
        foreach (Radiostation station in stations)
        {
            station.Initialize();
        }

        foreach (NodePath radioPath in radioListPath)
        {
            Radio radio = GetNode<Radio>(radioPath);
            if (monoStation) radio.Initialize(radiostation, frequencyRange, frequency);
            else radio.Initialize();
            radioList.Add(radio);
        }
    }

    public void EnterToRoom(List<Radio> roomRadioList)
    {
        foreach (Radio radio in radioList)
        {
            foreach (Radio roomRadio in roomRadioList)
            {
                if (radio == roomRadio) radio.RepeaterMode(false);
                else if (radio.isOn) radio.SetMute(true);
            }
        }
    }

    public void ExitFromRoom(List<Radio> roomRadioList)
    {
        foreach (Radio radio in radioList)
        {
            foreach (Radio roomRadio in roomRadioList)
            {
                if (radio == roomRadio) radio.RepeaterMode(true);
                else if (radio.isOn) radio.SetMute(false);
            }
        }
    }
}
