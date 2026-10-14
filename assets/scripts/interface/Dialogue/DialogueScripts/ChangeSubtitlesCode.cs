using Godot;

namespace DialogueScripts;

//Скрипт, меняющий субитировный код непися, с которым общается игрок
public class ChangeSubtitlesCode : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        GetNPC(node).subtitlesCode = parameter ?? "";
        Subtitles.MayClearCode = false;
    }
}