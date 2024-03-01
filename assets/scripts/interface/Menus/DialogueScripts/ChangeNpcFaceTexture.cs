namespace DialogueScripts
{
    public partial class ChangeNpcFaceTexture : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            NPCFace face = dialogueMenu.npc.GetNode<NPCFace>("Armature/Skeleton3D/Body");
            face?.ChangeEyesVariant(parameter);
        }
    }
}