namespace DialogueScripts
{
    public class ActivateTrigger : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter)
        {
            if (string.IsNullOrEmpty(parameter)) return;
            TriggerBase triggerToActivate = dialogueMenu.GetNode<TriggerBase>("/root/Main/Scene/triggers/" + parameter);
            triggerToActivate._on_activate_trigger();
        }
    }
}