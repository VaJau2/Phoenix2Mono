using Godot;

namespace DialogueScripts;

public class ChangeNpcMouthTexture : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        GetNPC(node)?
            .GetNode<NpcFace>("Armature/Skeleton/Body")?
            .ChangeMouthVariant(parameter);
    }
}