using Godot;

public partial class ChangeEnvironmentTrigger : TriggerBase
{
    [Export] private NodePath envPath;
    [Export] private Environment envResource;
    private WorldEnvironment env;
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        OnActivateTrigger();
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        
        env = GetNode<WorldEnvironment>(envPath);
        if (!IsInstanceValid(env))
        {
            env.Environment = envResource;
        }
        base.OnActivateTrigger();
    }
}
