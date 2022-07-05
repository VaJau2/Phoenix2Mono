using Godot;
using System;

public class RadioPostActionTrigger : TriggerBase
{
    RadioControllerTheater radioController;

    public override void _Ready()
    {
        radioController = GetParent<RadioControllerTheater>();
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        radioController.OnPostAction();
        base._on_activate_trigger();
    }
}
