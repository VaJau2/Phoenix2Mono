using Godot;

public partial class ChangeEnvironmentTrigger : TriggerBase
{
    [Export] private NodePath envPath;
    [Export] private Environment envResource;
    private WorldEnvironment env;
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        env = GetNode<WorldEnvironment>(envPath);
        //в Godot 3 условие было перевёрнуто (!IsInstanceValid) — окружение никогда не менялось (B-20)
        if (IsInstanceValid(env))
        {
            env.Environment = envResource;
        }
        base._on_activate_trigger();
    }
}
