using Godot;

namespace DialogueScripts;

public class NpcFollowPlayer : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        var npc = GetNPC(node);
        if (npc != null)
        {
            npc.followTarget = Global.Get().player;
        }
    }
}
