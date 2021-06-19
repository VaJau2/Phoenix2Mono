using System;
using Godot;
using Godot.Collections;

//триггер, который активирует другие триггеры
//только если досчитает до счетчика собственных активаций
public class TriggersConditionTrigger: ActivateOtherTrigger
{
    [Export] private int TriggersCount; 
    public int triggersCounter;

    public override Dictionary GetSaveData()
    {
        var returnData = base.GetSaveData();
        returnData.Add("triggersCount", triggersCounter);
        return returnData;
    }

    public override void LoadData(Dictionary data)
    {
        triggersCounter = Convert.ToInt32(data["triggersCount"]);
        base.LoadData(data);
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        triggersCounter++;
        if (triggersCounter != TriggersCount) return;
        
        base._on_activate_trigger();
    }
}