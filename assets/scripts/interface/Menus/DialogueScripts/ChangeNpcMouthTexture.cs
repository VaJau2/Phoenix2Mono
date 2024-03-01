namespace DialogueScripts
{
    public partial class ChangeNpcMouthTexture : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            NPCFace face = dialogueMenu.npc.GetNode<NPCFace>("Armature/Skeleton3D/Body");
            face?.ChangeMouthVariant(parameter);
        }
    }
}