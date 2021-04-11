using Godot;

public class TriggerBase : Area
{
    [Export] public bool DeleteAfterTrigger = true;

    public virtual void _on_body_entered(Node body)
    {
        if (!DeleteAfterTrigger) return;
        Global.AddDeletedObject(Name);
        QueueFree();
    }
}
