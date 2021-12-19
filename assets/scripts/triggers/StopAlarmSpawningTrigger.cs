using Godot;

//При активации выключает спавн врагов по тревоге
public class StopAlarmSpawningTrigger : ActivateOtherTrigger
{
    [Export] private NodePath managerPath;
    private EnemiesManager enemiesManager;

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        enemiesManager = GetNode<EnemiesManager>(managerPath);
        enemiesManager.maySpawn = !IsActive;
        base._on_activate_trigger();
    }
}
