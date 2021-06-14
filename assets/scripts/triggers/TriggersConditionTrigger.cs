using Godot;

//триггер, который активирует другие триггеры
//только если досчитает до счетчика собственных активаций
public class TriggersConditionTrigger: ActivateOtherTrigger
{
    [Export] private int TriggersCount; 
    public int triggersCounter;

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        triggersCounter++;
        if (triggersCounter != TriggersCount) return;
        
        base._on_activate_trigger();
    }
}