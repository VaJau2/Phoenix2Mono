using Godot;

public class StopAlarmButtonTrigger : TriggerBase, IInteractable
{
    [Export] public string HintCode = "clickButton";
    
    private EnemiesManager enemiesManager;
    
    public bool MayInteract => enemiesManager.isAlarming;
    public string InteractionHintCode => HintCode;
    
    public override void _Ready()
    {
        base._Ready();
        enemiesManager = GetNode<EnemiesManager>("/root/Main/Scene/npc");
    }
    
    public void Interact(PlayerCamera interactor)
    {
        enemiesManager.StopAlarm();
    }
}
