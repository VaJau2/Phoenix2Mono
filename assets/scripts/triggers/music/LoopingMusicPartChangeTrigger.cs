using Godot;

//Триггер позволяет изменить значения триггера LoopingMusicPartTrigger
public partial class LoopingMusicPartChangeTrigger : TriggerBase
{
    [Export] public float NewCheckTime;
    [Export] public float NewChangeTime;
    [Export] public NodePath triggerPath;
    private LoopingMusicPartTrigger trigger;

    public override void _Ready()
    {
        base._Ready();
        trigger = GetNode<LoopingMusicPartTrigger>(triggerPath);
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            OnActivateTrigger();
        }
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        if (NewChangeTime == NewCheckTime) return;
        trigger.ChangeTime = NewChangeTime;
        trigger.CheckTime = NewCheckTime;
        
        base.OnActivateTrigger();
    }
}
