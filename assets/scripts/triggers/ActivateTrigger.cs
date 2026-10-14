using Godot;
using System.Collections.Generic;

public class ActivateTrigger : TriggerBase
{
    [Export] private List<NodePath> nodePaths;
    private List<IActivated> nodes = new List<IActivated>();

    public override void _Ready()
    {
        if (nodePaths == null) return;
        
        foreach (var tempPath in nodePaths)
        {
            nodes.Add(GetNode<IActivated>(tempPath));
        }
    }
    
    public override void SetActive(bool value)
    {
        if (!IsActive) return;
        
        foreach (var node in nodes)
        {
            node.SetActive(true);
        }
            
        base._on_activate_trigger();
    }

    public override  void _on_activate_trigger()
    {
        SetActive(true);
    }
}