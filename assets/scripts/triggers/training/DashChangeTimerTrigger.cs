using Godot;

public partial class DashChangeTimerTrigger : TriggerBase
{
    [Export] public NodePath mainTriggerPath;
    [Export] public float newTimer = 5f;
    private DashTrainingTrigger mainTrigger;

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        mainTrigger = GetNode<DashTrainingTrigger>(mainTriggerPath);
        mainTrigger.StartTime = newTimer;
        base.OnActivateTrigger();
    }
}
