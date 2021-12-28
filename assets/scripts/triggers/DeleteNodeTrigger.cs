using Godot;
using Godot.Collections;

public class DeleteNodeTrigger : TriggerBase
{
    [Export] private Array<NodePath> objToDeletePaths;

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            _on_activate_trigger();
        }
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        foreach (var objToDeletePath in objToDeletePaths)
        {
            var objToDelete = GetNode(objToDeletePath);
            objToDelete.QueueFree();
        }
        
        base._on_activate_trigger();
    }
}
