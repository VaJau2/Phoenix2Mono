using Godot;

namespace DialogueScripts;

public class NpcDisappear : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        GetNPC(node)?.QueueFree();
    }
}