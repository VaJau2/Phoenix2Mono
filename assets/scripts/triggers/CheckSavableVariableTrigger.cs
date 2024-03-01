using System.Collections.Generic;
using Godot;

//проверяет квестовую переменную при активации и запускает другие триггеры в зависимости от её значения
//если активен при старте, проверяет сразу
public partial class CheckSavableVariableTrigger: TriggerBase
{
    [Export] public string variableName;
    [Export] public Godot.Collections.Dictionary<string, NodePath> otherTriggerPaths;
    
    private Godot.Collections.Dictionary<string, TriggerBase> otherTriggers = new();
    
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
            OnActivateTrigger();
        }
    }
    
    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        var saveNode =  GetNode<SaveNode>("/root/Main/SaveNode");
        if (saveNode == null) return;
        if (!saveNode.SavedVariables.ContainsKey(variableName)) return;

        string key = saveNode.SavedVariables[variableName].ToString();
        if (otherTriggers.ContainsKey(key))
        {
            otherTriggers[key].SetActive(true);
        }
     
        base.OnActivateTrigger();
    }
    
}