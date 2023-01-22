using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public class RadioController : Node, ISavable
{
    [Export] public bool playerInside;

    [Export] List<NodePath> radioListPath = new List<NodePath>();
    List<RadioBase> radioList = new List<RadioBase>();

    public string currentRoom;

    public override void _Ready()
    {
        var stations = GetChildren();
        foreach (Radiostation station in stations)
        {
            station.Initialize();
        }

        foreach (NodePath radioPath in radioListPath)
        {
            RadioBase radio = GetNode<RadioBase>(radioPath);
            radio.Initialize();
            radioList.Add(radio);
        }
    }

    public void EnterToRoom(List<RadioBase> roomRadioList)
    {
        List<RadioBase> outerRadioList = radioList;

        foreach (RadioBase roomRadio in roomRadioList)
        {
            roomRadio.RepeaterMode(false);
            outerRadioList.Remove(roomRadio);
        }

        foreach (RadioBase radio in outerRadioList)
        {
            radio.SetMute(true);
        }
    }

    public void ExitFromRoom(List<RadioBase> roomRadioList)
    {
        List<RadioBase> outerRadioList = radioList;

        foreach (RadioBase roomRadio in roomRadioList)
        {
            roomRadio.RepeaterMode(true);
            outerRadioList.Remove(roomRadio);
        }

        foreach (RadioBase radio in outerRadioList)
        {
            radio.SetMute(false);
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"current_room", currentRoom}
        };
    }

    public void LoadData(Dictionary data)
    {
        if (!data.Contains("current_room")) return;
        currentRoom = (string)data["current_room"];

        if (!string.IsNullOrEmpty(currentRoom))
        {
            Room room = GetNode<Room>(currentRoom);
            room.Enter();
        }
    }
}
