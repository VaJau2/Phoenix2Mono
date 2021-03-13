//Скрипт, меняющий диалоговый код непися, с которым общается игрок
public class ChangeDialogueCode: IDialogueScript {
    public void initiate(DialogueMenu dialogueMenu, string parameter)
    {
        dialogueMenu.npc.dialogueCode = parameter;
    }
}