using Godot;

namespace DialogueScripts;

public class ActivateCinematic : IDialogueScript
{
    public void initiate(Node node, string parameter, string key = "")
    {
        if (string.IsNullOrEmpty(parameter)) return;
        
        var cutscene = node.GetNode<TriggerBase>($"/root/Main/Scene/cutscenes/{key}/{parameter}");
        cutscene._on_activate_trigger();
    }
}