using Godot;
using Godot.Collections;

//активирует другие триггеры
public class ActivateOtherTrigger: TriggerBase
{
    [Export] public Array<NodePath> otherTriggerPaths;
    private Array<TriggerBase> otherTriggers = new Array<TriggerBase>();

    public override void _Ready()
    {
        foreach(NodePath tempPath in otherTriggerPaths)
        {
            otherTriggers.Add(GetNode<TriggerBase>(tempPath));
        }
    }

    public void _on_activate_trigger()
    {
        if (IsActive)
        {
            foreach (TriggerBase otherTrigger in otherTriggers)
            {
                otherTrigger.SetActive(true);
            }
            if (DeleteAfterTrigger)
            {
                QueueFree();
            }
        }
    }
}
