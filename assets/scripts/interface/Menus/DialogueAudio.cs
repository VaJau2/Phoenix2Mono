using Godot;

//Ищет по пути /audio/dialogues/lang/npc/dialogue/ озвучку для диалогового нода
//Проигрывает, если файл существует
//Если не существует, озвучивает через динамическую озвучку
class DialogueAudio: AudioStreamPlayer
{
    private const string FILE_EXT = "mp3";
    
    private string characterName;
    private string dialogueCode;
    private bool foundFile;

    private DialogueAudioConfig config = new DialogueAudioConfig();
    private int visibleChars;
    private bool isSilent;

    private readonly float startPitch;

    public DialogueAudio()
    {
        startPitch = PitchScale;
    }

    public void LoadCharacter(string newCharacterName, string newDialogueCode, string customConfig = null)
    {
        characterName = newCharacterName;
        dialogueCode = newDialogueCode;
        config.LoadConfig(customConfig ?? newCharacterName);
        visibleChars = 0;
        isSilent = false;
    }

    public void TryToPlayAudio(string nodeCode)
    {
        string path = $"assets/audio/dialogues/{characterName}/{dialogueCode}/{nodeCode}.{FILE_EXT}";
        foundFile = ResourceLoader.Exists(path);
        if (!foundFile)
        {
            return;
        }

        PitchScale = startPitch;
        Stream = GD.Load<AudioStream>(path);
        Play();
    }

    public void UpdateDynamicPlaying(char symbol)
    {
        if (foundFile && characterName != "strikely")
        {
            return;
        }
        
        visibleChars += 1;
        switch (symbol)
        {
            case ' ': return;
            case '\n': return;
            case '.': return;
            case '[':
                isSilent = true;
                return;
            case ']' when isSilent:
                isSilent = false;
                return;
        }

        if (isSilent)
        {
            return;
        }

        if (visibleChars % config.CharsToSound != 0)
        {
            return;
        }
        
        ChooseSound(symbol);
        Play();
    }

    private void ChooseSound(char symbol)
    {
        var symbolHash = symbol.GetHashCode();
        var soundI = symbolHash % config.Sounds.Count;
        var sound = config.Sounds[soundI];
        Stream = sound;

        var minPitchInt = (int)(config.MinPitch * 100f);
        var maxPitchInt = (int)(config.MaxPitch * 100f);
        var pitchRange = maxPitchInt - minPitchInt;
        if (pitchRange != 0)
        {
            var tempPitch = (symbolHash % pitchRange) + minPitchInt;
            PitchScale = Mathf.Abs(tempPitch / 100f);
        }
        else
        {
            PitchScale = config.MinPitch;
        }
    }
}