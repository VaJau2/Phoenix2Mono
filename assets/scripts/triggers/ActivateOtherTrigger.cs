using Godot;

//активирует другой триггер
public class ActivateOtherTrigger: Node
{
    [Export] public NodePath otherTriggerPath;
    private TriggerBase otherTrigger;

    public override void _Ready()
    {
        otherTrigger = GetNode<TriggerBase>(otherTriggerPath);
    }

    public void _on_activate_trigger()
    {
        otherTrigger.IsActive = true;
    }
}
