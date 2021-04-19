using System;
using Godot;
using Godot.Collections;

public class TriggerBase : Area, ISavable
{
    [Export] public bool IsActive = true;
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

    public virtual void _on_body_entered(Node body)
    {
        if (!DeleteAfterTrigger) return;
        Global.AddDeletedObject(Name);
        QueueFree();
    }
}
