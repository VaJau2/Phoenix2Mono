using Godot;

namespace DialogueScripts;

//Скрипт, меняющий субитировный код непися, с которым общается игрок
public class ChangeInSubtitlesCode : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        GD.Print(GetNPC(node));
        GetNPC(node).subtitlesCode = parameter ?? "";
    }
}