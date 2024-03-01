namespace DialogueScripts
{
    public partial class ActivateTrigger : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(parameter)) return;
            TriggerBase triggerToActivate = dialogueMenu.GetNode<TriggerBase>("/root/Main/Scene/triggers/" + parameter);
            triggerToActivate.OnActivateTrigger();
        }
    }
}