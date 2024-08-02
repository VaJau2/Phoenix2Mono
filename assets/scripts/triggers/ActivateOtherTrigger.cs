using Godot;
using Godot.Collections;

//включает другие триггеры
public class ActivateOtherTrigger: TriggerBase
{
    [Export] public Array<NodePath> otherTriggerPaths;
    [Export] public Array<NodePath> disactiveTriggerPaths;
    
    protected Array<TriggerBase> otherTriggers = [];
    protected Array<TriggerBase> triggersToDisactive = [];

    public override void _Ready()
    {
        if (otherTriggerPaths != null)
        {
            InitializedTriggers(otherTriggerPaths, otherTriggers);
        }

        if (disactiveTriggerPaths != null)
        {
            InitializedTriggers(disactiveTriggerPaths, triggersToDisactive);
        }
    }

    private void InitializedTriggers(Array<NodePath> paths, Array<TriggerBase> triggers)
    {
        foreach(var path in paths)
        {
            var trigger = GetNodeOrNull<TriggerBase>(path);
            if (trigger != null) triggers.Add(trigger);
        }
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        foreach (var otherTrigger in otherTriggers)
        {
            otherTrigger?.SetActive(true);
        }
            
        foreach (var otherTrigger in triggersToDisactive)
        {
            otherTrigger?.SetActive(false);
        }
        
        base._on_activate_trigger();
    }

    public virtual void _on_body_entered(Node body)
    {
        if (body is Player)
        {
            _on_activate_trigger();
        }
    }
}
