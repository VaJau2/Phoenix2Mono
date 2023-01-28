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
    
    EnemiesManager enemiesManager;

    public override void _Ready()
    {
        var stations = GetChildren();
        foreach (Radiostation station in stations)
        {
            station.Initialize();
        }

        for (int i = 0; i < radioListPath.Count; i++)
        {
            RadioBase radio = GetNode<RadioBase>(radioListPath[i]);
            if (radio is Receiver)
            {
                radio.Initialize();
                radioList.Add(radio);
                radioListPath.Remove(radioListPath[i]);
            }
        }

        foreach (NodePath radioPath in radioListPath)
        {
            RadioBase radio = GetNode<RadioBase>(radioPath);
            radio.Initialize();
            radioList.Add(radio);
        }

        radioListPath.Clear();

        enemiesManager = GetNodeOrNull<EnemiesManager>("/root/Main/Scene/npc");
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmStarted), this, nameof(OnAlarmStart));
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmEnded), this, nameof(OnAlarmEnd));
    }

    public void EnterToRoom(List<RadioBase> roomRadioList)
    {
        List<RadioBase> outerRadioList = new List<RadioBase>();
        outerRadioList.AddRange(radioList);

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
        List<RadioBase> outerRadioList = new List<RadioBase>();
        outerRadioList.AddRange(radioList);

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

    void OnAlarmStart()
    {
        foreach (RadioBase radio in radioList)
        {
            if (radio is Receiver receiver && receiver.isOn)
            {
                receiver.SwitchOff();
                receiver.AddToGroup("Alarm Mode");
            }
        }
    }

    void OnAlarmEnd()
    {
        foreach (Node node in GetTree().GetNodesInGroup("Alarm Mode"))
        {
            if (node is Receiver receiver) receiver.SwitchOn();
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
