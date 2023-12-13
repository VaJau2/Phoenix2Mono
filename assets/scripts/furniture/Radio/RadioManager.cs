using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class RadioManager : Node, ISavable
{
    [Export] public bool playerInside;

    [Export] private List<NodePath> radioListPath = new ();
    protected List<RadioBase> radioList = new ();

    public string currentRoom;

    public override void _Ready()
    {
        foreach (var children in GetChildren())
        {
            if (children is Radiostation station)
            {
                station.Initialize();
            }
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
    }

    public void EnterToRoom(List<RadioBase> roomRadioList)
    {
        var outerRadioList = new List<RadioBase>();
        outerRadioList.AddRange(radioList);
        
        foreach (var roomRadio in roomRadioList)
        {
            
            roomRadio.RepeaterMode(false);
            outerRadioList.Remove(roomRadio);
        }

        foreach (var radio in outerRadioList)
        {
            radio.SetMute(true);
        }
    }

    public void ExitFromRoom(List<RadioBase> roomRadioList)
    {
        var outerRadioList = new List<RadioBase>();
        outerRadioList.AddRange(radioList);

        foreach (var roomRadio in roomRadioList)
        {
            roomRadio.RepeaterMode(true);
            outerRadioList.Remove(roomRadio);
        }

        foreach (var radio in outerRadioList)
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
            var room = GetNode<Room>(currentRoom);
            room.Enter();
        }
    }
}
