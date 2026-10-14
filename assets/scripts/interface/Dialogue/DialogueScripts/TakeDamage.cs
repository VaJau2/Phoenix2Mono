using Godot;

namespace DialogueScripts
{
    public class TakeDamage : IDialogueScript
    {
        public void initiate(Node node, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(parameter)) return;
            var player = Global.Get().player;
            player.TakeDamage(player, int.Parse(parameter));
        }
    }
}