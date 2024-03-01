using Godot;
using Godot.Collections;

public partial class RadioManager : Node
{
    [Export] private Array<NodePath> radioListPath = [];
    protected Array<RadioBase> radioList = [];

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

    public void EnterToRoom(Array<RadioBase> roomRadioList)
    {
        var outerRadioList = new Array<RadioBase>();
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

    public void ExitFromRoom(Array<RadioBase> roomRadioList)
    {
        var outerRadioList = new Array<RadioBase>();
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
}
