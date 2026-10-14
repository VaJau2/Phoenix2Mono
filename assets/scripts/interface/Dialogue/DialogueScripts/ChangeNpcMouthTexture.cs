using Godot;

namespace DialogueScripts;

public partial class ChangeNpcMouthTexture : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        GetNPC(node)?
            .GetNode<NpcFace>("Armature/Skeleton3D/Body")?
            .ChangeMouthVariant(parameter);
    }
}