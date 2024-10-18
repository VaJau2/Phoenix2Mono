using Godot;

namespace DialogueScripts;

public class ChangeNpcFaceTexture : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        GetNPC(node)?
            .GetNode<NpcFace>("Armature/Skeleton/Body")?
            .ChangeEyesVariant(parameter);
    }
}