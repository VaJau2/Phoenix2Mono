using Godot;

public class StopAlarmButtonTrigger : TriggerBase
{
    [Export] public string HintCode = "clickButton";
    private EnemiesManager enemiesManager;
    private bool checkButton => enemiesManager.isAlarming;
    private static Player player => Global.Get().player;
    
    public override void _Ready()
    {
        base._Ready();
        enemiesManager = GetNode<EnemiesManager>("/root/Main/Scene/npc");
        SetProcess(false);
    }
    
    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        if (!checkButton) return;
        SetProcess(true);
    }
    
    public void _on_body_exited(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        if (!checkButton) return;
        player?.Camera.HideHint();
        SetProcess(false);
    }
    
    public override void _Process(float delta)
    {
        player.Camera.ShowHint(HintCode, false);

        if (!Input.IsActionJustPressed("use")) return;
        player?.Camera.HideHint();
        SetProcess(false);
        enemiesManager.StopAlarm();
    }
}
