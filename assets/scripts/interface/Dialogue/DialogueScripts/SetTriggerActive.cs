using Godot;

namespace DialogueScripts
{
    public class SetTriggerActive : IDialogueScript
    {
        public void initiate(Node node, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(parameter)) return;
            TriggerBase triggerToActivate = node.GetNode<TriggerBase>("/root/Main/Scene/triggers/" + parameter);
            triggerToActivate.SetActive(true);
        }
    }
}