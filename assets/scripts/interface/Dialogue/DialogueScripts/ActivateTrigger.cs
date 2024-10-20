using Godot;

namespace DialogueScripts;

public class ActivateTrigger : IDialogueScript
{
    public void initiate(Node node, string parameter, string key = "")
    {
        if (string.IsNullOrEmpty(parameter)) return;
        
        var triggerToActivate = node.GetNode<TriggerBase>("/root/Main/Scene/triggers/" + parameter);
        triggerToActivate._on_activate_trigger();
    }
}