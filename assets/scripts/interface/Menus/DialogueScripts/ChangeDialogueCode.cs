namespace DialogueScripts
{
//Скрипт, меняющий диалоговый код непися, с которым общается игрок
    public partial class ChangeDialogueCode : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            dialogueMenu.npc.dialogueCode = parameter ?? "";
        }
    }
}