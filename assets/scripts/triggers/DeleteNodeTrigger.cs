using Godot;
using Godot.Collections;

public partial class DeleteNodeTrigger : TriggerBase
{
    [Export] private Array<NodePath> objToDeletePaths;

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

        foreach (var objToDeletePath in objToDeletePaths)
        {
            var objToDelete = GetNode(objToDeletePath);
            objToDelete.QueueFree();
        }
        
        base.OnActivateTrigger();
    }
}
