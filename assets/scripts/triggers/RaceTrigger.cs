using System;
using Godot;

public class RaceTrigger : TriggerBase
{
    [Export] private NodePath earthponyTriggerPath;
    [Export] private NodePath pegasusTriggerPath;
    [Export] private NodePath unicornTriggerPath;
    [Export] private Mode mode;
    
    private enum Mode
    {
        ActivateTrigger,
        SetActiveTrigger
    }
    
    public override async void _Ready()
    {
        await ToSignal(GetTree(), "idle_frame");
        if (IsActive) _on_activate_trigger();
    }
    
    public override void _on_activate_trigger()
    {
        var path = Global.Get().playerRace switch
        {
            Race.Earthpony => earthponyTriggerPath,
            Race.Pegasus => pegasusTriggerPath,
            Race.Unicorn => unicornTriggerPath,
            _ => throw new ArgumentOutOfRangeException()
        };

        var trigger = GetNode<TriggerBase>(path);

        switch (mode)
        {
            case Mode.ActivateTrigger:
                trigger._on_activate_trigger();
                break;
            
            case Mode.SetActiveTrigger:
                trigger.SetActive(true);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        base._on_activate_trigger();
    }
}
