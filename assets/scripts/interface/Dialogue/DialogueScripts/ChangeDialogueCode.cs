using Godot;

namespace DialogueScripts;

//Скрипт, меняющий диалоговый код непися, с которым общается игрок
public class ChangeDialogueCode : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        GetNPC(node).dialogueCode = parameter ?? "";
    }
}