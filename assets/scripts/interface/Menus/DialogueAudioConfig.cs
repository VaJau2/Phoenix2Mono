using Godot;
using Godot.Collections;

public partial class DialogueAudioConfig
{
    private readonly Array defaultSoundCodes = new Array
    {
        "sound_a", "sound_e", "sound_i", "sound_o", "sound_u", "sound_y"
    };
    
    private const float DEFAULT_MIN_PITCH = 1f;
    private const float DEFAULT_MAX_PITCH = 2f;
    private const int DEFAULT_CHARS_TO_SOUND = 3;

    public Array<AudioStreamWav> Sounds { get; } = new();
    public float MinPitch { get; private set; } = 0.5f;
    public float MaxPitch { get; private set; } = 3f;
    public int CharsToSound { get; private set; } = 3;

    private string tempCode;

    public void LoadConfig(string code)
    {
        if (code == null)
        {
            code = "";
        }
        
        if (tempCode == code) return;
        
        var path = $"assets/dialogues/configs/{code}.json";
        if (!FileAccess.FileExists("res://" + path))
        {
            LoadDefaultConfig();
            return;
        }

        var configFile = Global.LoadJsonFile(path);
        
        if (configFile.TryGetValue("sounds", out var soundsValue))
        {
            var sounds = soundsValue.AsGodotDictionary();
            LoadSounds(sounds.Values as Array);
        }

        if (configFile.TryGetValue("min_pitch", out var minPitchValue))
        {
            MinPitch = Global.ParseFloat(minPitchValue.ToString());
        }
        
        if (configFile.TryGetValue("max_pitch", out var maxPitchValue))
        {
            MaxPitch = Global.ParseFloat(maxPitchValue.ToString());
        }
        
        if (configFile.TryGetValue("chars_to_sound", out var charsToSoundValue))
        {
            CharsToSound = charsToSoundValue.AsInt32();
        }

        tempCode = code;
    }

    private void LoadDefaultConfig()
    {
        LoadSounds(defaultSoundCodes);
        MinPitch = DEFAULT_MIN_PITCH;
        MaxPitch = DEFAULT_MAX_PITCH;
        CharsToSound = DEFAULT_CHARS_TO_SOUND;
        tempCode = "";
    }

    private void LoadSounds(Array soundCodes)
    {
        Sounds.Clear();
        foreach (string soundCode in soundCodes)
        {
            var sound = GD.Load<AudioStreamWav>($"res://assets/audio/dialogues/dynamic/{soundCode}.wav");
            Sounds.Add(sound);
        }
    }
}
