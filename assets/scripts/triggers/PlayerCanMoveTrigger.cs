using Godot;
using System;

public class PlayerCanMoveTrigger : ActivateOtherTrigger
{
    [Export] public bool MakeMayMove;
    
    Player player => Global.Get().player;
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        player.MayMove = MakeMayMove;
        base._on_activate_trigger();
    }

    public void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        _on_activate_trigger();
    }
    
    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }
}
