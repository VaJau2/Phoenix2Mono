using Godot;
using Godot.Collections;

public class SavableTimers: Node, ISavable
{
    private Dictionary<string, float> timers = new Dictionary<string, float>();

    public bool CheckTimer(string timerName, float timerLength)
    {
        if (timerLength == 0) return false;
        
        if (timers.ContainsKey(timerName))
        {
            return timers[timerName] > 0;
        }

        timers.Add(timerName, timerLength);
        return true;
    }

    public override void _Process(float delta)
    {
        if (timers.Count <= 0) return;
        foreach (var timerKey in timers.Keys)
        {
            if (timers[timerKey] > 0)
            {
                timers[timerKey] -= delta;
            }       
        }
    }

    public Dictionary GetSaveData()
    {
        var savableData = new Dictionary();

        foreach (string timerName in timers.Keys)
        {
            savableData.Add(timerName, timers[timerName]);
        }

        return savableData;
    }

    public void LoadData(Dictionary data)
    {
        timers = new Dictionary<string, float>(data);
    }
}