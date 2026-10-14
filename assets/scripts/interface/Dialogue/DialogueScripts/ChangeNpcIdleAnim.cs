using Godot;

namespace DialogueScripts;

public partial class ChangeNpcIdleAnim : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        var body = GetNPC(node)?.GetNodeOrNull<PonyBody>("body");
        if (body != null)
        {
            body.IdleAnim = parameter;
        }
    }
}