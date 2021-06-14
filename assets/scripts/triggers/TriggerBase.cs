using System;
using Godot;
using Godot.Collections;

public class TriggerBase : Node, ISavable
{
    [Export] public bool IsActive { get; private set; } = true;
    [Export] public bool DeleteAfterTrigger = true;

    public Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"active", IsActive}
        };
    }
    public void LoadData(Dictionary data)
    {
        IsActive = Convert.ToBoolean(data["active"]);
    }

    //включает триггер (не активирует)
    public virtual void SetActive(bool newActive)
    {
        IsActive = newActive;
    }
    
    //событие активации триггера
    public virtual void _on_activate_trigger()
    {
        if (!DeleteAfterTrigger) return;
        Global.AddDeletedObject(Name);
        QueueFree();
        IsActive = false;
    }
}
