namespace DialogueScripts
{
    public partial class NpcDisappear : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            dialogueMenu.npc.QueueFree();
        }
    }
}