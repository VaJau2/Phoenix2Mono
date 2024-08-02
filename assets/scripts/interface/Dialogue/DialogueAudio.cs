using Godot;

//Ищет по пути /audio/dialogues/lang/npc/dialogue/ озвучку для диалогового нода
//Проигрывает, если файл существует
//Если не существует, озвучивает через динамическую озвучку
class DialogueAudio: Node
{
    [Export] private NodePath audioPlayerPath;
    private string tempAudioPath;
    private AudioPlayerCommon audioPlayer;
    
    private string characterName;
    private string dialogueCode;
    
    private readonly string[] fileExt = [".mp3", ".ogg", ".wav"];
    private bool foundFile;

    private DialogueAudioConfig config = new();
    private int visibleChars;
    private bool isSilent;

    private float startPitch;

    public override void _Ready()
    {
        if (audioPlayerPath == null) return;
        
        var newPlayer = GetNode(audioPlayerPath);
        SetAudioPlayer(newPlayer);
    }

    public void SetAudioPlayer(Node newPlayer)
    {
        tempAudioPath = newPlayer.GetPath();
        audioPlayer = new AudioPlayerCommon(newPlayer);
        startPitch = audioPlayer.PitchScale;
    }

    public string GetAudioPlayerPath()
    {
        return tempAudioPath;
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
        var path = $"assets/audio/dialogues/{characterName}/{dialogueCode}/{nodeCode}";
        foundFile = false;

        foreach (var ext in fileExt)
        {
            if (!ResourceLoader.Exists(path + ext)) continue;
            
            path += ext;
            foundFile = true;
            break;
        }
        
        if (!foundFile) return;

        audioPlayer.PitchScale = startPitch;
        audioPlayer.Stream = GD.Load<AudioStream>(path);
        audioPlayer.Play();
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
            case '?': return;
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
        audioPlayer.Play();
    }

    public void Stop()
    {
        audioPlayer.Stop();
    }

    private void ChooseSound(char symbol)
    {
        var symbolHash = symbol.GetHashCode();
        var soundI = symbolHash % config.Sounds.Count;
        var sound = config.Sounds[soundI];
        audioPlayer.Stream = sound;

        var minPitchInt = (int)(config.MinPitch * 100f);
        var maxPitchInt = (int)(config.MaxPitch * 100f);
        var pitchRange = maxPitchInt - minPitchInt;
        if (pitchRange != 0)
        {
            var tempPitch = (symbolHash % pitchRange) + minPitchInt;
            audioPlayer.PitchScale = Mathf.Abs(tempPitch / 100f);
        }
        else
        {
            audioPlayer.PitchScale = config.MinPitch;
        }
    }
}