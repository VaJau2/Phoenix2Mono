namespace DialogueScripts
{
    public class ChangeNpcFaceTexture : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            NPCFace face = dialogueMenu.npc.GetNode<NPCFace>("Armature/Skeleton/Body");
            face?.ChangeEyesVariant(parameter);
        }
    }
}