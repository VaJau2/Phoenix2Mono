using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class RadioManager : Node, ISavable
{
    [Export] public bool playerInside;

    [Export] private List<NodePath> radioListPath = new ();
    private List<RadioBase> radioList = new ();

    public string currentRoom;
    
    private EnemiesManager enemiesManager;

    public override void _Ready()
    {
        var stations = GetChildren();
        foreach (Radiostation station in stations)
        {
            station.Initialize();
        }
        
        for (int i = 0; i < radioListPath.Count; i++)
        {
            var radio = GetNode<RadioBase>(radioListPath[i]);
            radio.RadioManager = this;

            if (radio is not Receiver) continue;
            
            radio.Initialize();
            radioList.Add(radio);
            radioListPath.RemoveAt(i);
            i--;
        }

        foreach (var radioPath in radioListPath)
        {
            var speaker = GetNode<RadioBase>(radioPath);
            speaker.Initialize();
            radioList.Add(speaker);
        }
        
        radioListPath.Clear();

        enemiesManager = GetNodeOrNull<EnemiesManager>("/root/Main/Scene/npc");
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmStarted), this, nameof(OnAlarmStart));
        enemiesManager?.Connect(nameof(EnemiesManager.AlarmEnded), this, nameof(OnAlarmEnd));
    }

    public void EnterToRoom(List<RadioBase> roomRadioList)
    {
        var outerRadioList = new List<RadioBase>();
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

    private void OnAlarmStart()
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

    private void OnAlarmEnd()
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
            var room = GetNode<Room>(currentRoom);
            room.Enter();
        }
    }
}
