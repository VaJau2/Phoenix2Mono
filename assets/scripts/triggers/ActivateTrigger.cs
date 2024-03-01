using Godot;
using System.Collections.Generic;
using Godot.Collections;

public partial class ActivateTrigger : TriggerBase
{
    [Export] private Array<NodePath> nodePaths;
    private List<IActivated> nodes = [];

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
            
        base.OnActivateTrigger();
    }

    public override  void OnActivateTrigger()
    {
        SetActive(true);
    }
}