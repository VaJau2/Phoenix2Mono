using Godot;
using System.Collections.Generic;

//проверяет квестовую переменную при активации и запускает другие триггеры в зависимости от её значения
//если активен при старте, проверяет сразу
public class CheckSavableVariableTrigger: TriggerBase
{
    [Export] public string variableName;
    [Export] public Dictionary<string, NodePath> otherTriggerPaths;
    
    private Dictionary<string, TriggerBase> otherTriggers = new Dictionary<string, TriggerBase>();
    
    public override void _Ready()
    {
        if (otherTriggerPaths == null) return;
        foreach (KeyValuePair<string, NodePath> otherTrigger in otherTriggerPaths)
        {
            TriggerBase trigger = GetNode<TriggerBase>(otherTrigger.Value);
            otherTriggers.Add(otherTrigger.Key, trigger);
        }

        if (IsActive)
        {
            _on_activate_trigger();
        }
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        var saveNode =  GetNode<SaveNode>("/root/Main/SaveNode");
        if (saveNode == null) return;
        if (!saveNode.SavedVariables.Contains(variableName)) return;

        string key = saveNode.SavedVariables[variableName].ToString();
        if (otherTriggers.ContainsKey(key))
        {
            otherTriggers[key].SetActive(true);
        }
     
        base._on_activate_trigger();
    }
    
}