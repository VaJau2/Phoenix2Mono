using Godot;
using Godot.Collections;

public class CheckNpcEnterTrigger : ActivateOtherTrigger
{
    [Export] private Array<string> npcNames;

    public override void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!npcNames.Contains(body.Name)) return;
        _on_activate_trigger();
    }
}
