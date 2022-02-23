using Godot;

//Ищет по пути /audio/dialogues/lang/npc/dialogue/ озвучку для диалогового нода
//Проигрывает, если файл существует
class DialogueAudio: AudioStreamPlayer
{
    const string FILE_EXT = "mp3";

    private string lang;
    private string npcName;
    private string dialogueCode;

    public void LoadNpc(string newNpcName, string newDialogueCode)
    {
        lang = InterfaceLang.GetLang();
        npcName = newNpcName;
        dialogueCode = newDialogueCode;
    }

    public void TryToPlayAudio(string nodeCode)
    {
        string path = string.Format(
            "res://assets/audio/dialogues/{0}/{1}/{2}/{3}.{4}", 
            lang, npcName, dialogueCode, nodeCode, FILE_EXT
        );

        if (!ResourceLoader.Exists(path)) return;
        Stream = GD.Load<AudioStream>(path);
        Play();
    }
}