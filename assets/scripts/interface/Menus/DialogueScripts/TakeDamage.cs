namespace DialogueScripts
{
    public partial class TakeDamage : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(parameter)) return;
            var player = dialogueMenu.player;
            player.TakeDamage(player, int.Parse(parameter));
        }
    }
}