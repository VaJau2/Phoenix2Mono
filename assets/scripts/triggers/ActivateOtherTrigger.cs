using Godot;
using Godot.Collections;

//включает другие триггеры
public class ActivateOtherTrigger: TriggerBase
{
    [Export] public Array<NodePath> otherTriggerPaths;
    [Export] public Array<NodePath> disactiveTriggerPaths;
    protected Array<TriggerBase> otherTriggers = new Array<TriggerBase>();
    protected Array<TriggerBase> triggersToDisactive = new Array<TriggerBase>();

    public override void _Ready()
    {
        if (otherTriggerPaths != null)
        {
            foreach (NodePath tempPath in otherTriggerPaths)
            {
                otherTriggers.Add(GetNode<TriggerBase>(tempPath));
            }
        }

        if (disactiveTriggerPaths != null)
        {
            foreach(NodePath tempPath in disactiveTriggerPaths)
            {
                triggersToDisactive.Add(GetNode<TriggerBase>(tempPath));
            }
        }
       
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        foreach (TriggerBase otherTrigger in otherTriggers)
        {
            otherTrigger.SetActive(true);
        }
            
        foreach (TriggerBase otherTrigger in triggersToDisactive)
        {
            otherTrigger.SetActive(false);
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
