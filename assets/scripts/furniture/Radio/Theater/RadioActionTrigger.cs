using Godot;
using System;

public class RadioActionTrigger : TriggerBase
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
        radioController.OnAction();
        base._on_activate_trigger();
    }
}
