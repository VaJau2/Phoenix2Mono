using System;
using Godot;
using Godot.Collections;

public partial class TriggerBase : Node, ISavable, IActivated
{
    [Export] public bool IsActive { get; private set; } = true;
    [Export] public bool DeleteAfterTrigger = true;

    public virtual Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"active", IsActive}
        };
    }
    public virtual void LoadData(Dictionary data)
    {
        IsActive = Convert.ToBoolean(data["active"]);
    }

    //включает триггер (не активирует)
    public virtual void SetActive(bool newActive)
    {
        IsActive = newActive;
    }
    
    //событие активации триггера
    public virtual void OnActivateTrigger()
    {
        if (!DeleteAfterTrigger) return;
        Global.AddDeletedObject(Name);
        QueueFree();
        IsActive = false;
    }
}
