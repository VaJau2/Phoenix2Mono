namespace DialogueScripts
{
    public class ChangeNpcMouthTexture : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            NPCFace face = dialogueMenu.npc.GetNode<NPCFace>("Armature/Skeleton/Body");
            face?.ChangeMouthVariant(parameter);
        }
    }
}