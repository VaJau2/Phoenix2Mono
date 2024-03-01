using Godot;
using Godot.Collections;
using System.Linq;

public partial class TriggerInteractionAdapter : StaticBody3D, IInteractable
{
    private const string DefaultHint = "use";
    
    [Export] private Array<string> hintCodes;
    [Export] private Array<NodePath> activateTriggerPaths;

    private Dictionary triggersData = new Dictionary();
    
    private Array<TriggerBase> triggers = [];
    
    public bool MayInteract => GetActiveTriggerIndex() != -1;

    public string InteractionHintCode
    {
        get
        {
            var index = GetActiveTriggerIndex();
            return hintCodes.ElementAtOrDefault(index) != null 
                ? hintCodes[index]
                : DefaultHint;
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

        triggers[index].OnActivateTrigger();
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