using Godot;

namespace DialogueScripts
{
    public class SetNpcFollowWaiting: BaseChangeInNPC
    {
        public override void initiate(Node node, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(parameter)) return;
            
            var npc = GetNPC(node);
            var npcState = npc?.GetStateClass();
            
            if (npc != null && npcState is FollowState followState)
            {
                followState.SetWaiting(parameter == "true");
            }
        }
    }
}