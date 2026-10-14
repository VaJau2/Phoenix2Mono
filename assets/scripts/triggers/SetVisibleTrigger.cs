using Godot;
using Godot.Collections;

public class SetVisibleTrigger : TriggerBase
{
    [Export] private Array<NodePath> showObjectsPath = [];
    [Export] private Array<NodePath> hideObjectsPath = [];

    public override void SetActive(bool value)
    {
        base.SetActive(value);
        if (IsActive) _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        SetVisible(showObjectsPath, true);
        SetVisible(hideObjectsPath, false);
    }

    private void SetVisible(Array<NodePath> objectsPath, bool value)
    {
        foreach (var path in objectsPath)
        {
            GetNode<Spatial>(path).Visible = value;
        }
    }
}
