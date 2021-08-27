namespace DialogueScripts
{
//Скрипт, меняющий диалоговый код непися, с которым общается игрок
    public class ChangeDialogueCode : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            dialogueMenu.npc.dialogueCode = parameter ?? "";
        }
    }
}