using Godot;

namespace DialogueScripts;

public partial class NpcDisappear : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        GetNPC(node)?.QueueFree();
    }
}