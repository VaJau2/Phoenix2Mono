using Godot;

public class DashChangeTimerTrigger : TriggerBase
{
    [Export] public NodePath mainTriggerPath;
    [Export] public float newTimer = 5f;
    private DashTrainingTrigger mainTrigger;

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        mainTrigger = GetNode<DashTrainingTrigger>(mainTriggerPath);
        mainTrigger.StartTime = newTimer;
        base._on_activate_trigger();
    }
}
