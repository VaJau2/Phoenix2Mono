using Godot;
using System.Linq;
using Godot.Collections;
using System.Collections.Generic;

public class TriggerInteractionAdapter : StaticBody, IInteractable
{
    private const string DEFAULT_HINT = "use";
    
    [Export] private List<string> hintCodes;
    [Export] private List<NodePath> activateTriggerPaths;

    private Dictionary triggersData = new Dictionary();
    
    private List<TriggerBase> triggers = new List<TriggerBase>();
    
    public bool MayInteract => GetActiveTriggerIndex() != -1;

    public string InteractionHintCode
    {
        get
        {
            var index = GetActiveTriggerIndex();
            return hintCodes.ElementAtOrDefault(index) != null 
                ? hintCodes[index]
                : DEFAULT_HINT;
        }
    }

    public override void _Ready()
    {
        int index = 0;
        
        foreach (var path in activateTriggerPaths)
        {
            var trigger = GetNodeOrNull<TriggerBase>(path);
            
            if (trigger != null) triggers.Add(trigger);
            else hintCodes.RemoveAt(index);
            
            index++;
        }
    }

    public void Interact(PlayerCamera interactor)
    {
        var index = GetActiveTriggerIndex();
        
        if (index < 0) return;

        triggers[index]._on_activate_trigger();
        triggers.Remove(triggers[index]);
        hintCodes.Remove(hintCodes[index]);
    }

    private int GetActiveTriggerIndex()
    {
        for (int i = 0; i < triggers.Count; i++)
        {
            if (triggers[i].IsActive) return i;
        }

        return -1;
    }
}