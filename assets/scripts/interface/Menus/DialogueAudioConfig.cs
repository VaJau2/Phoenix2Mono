using Godot;
using Godot.Collections;

public class DialogueAudioConfig
{
    private readonly Array defaultSoundCodes = new Array
    {
        "sound_a", "sound_e", "sound_i", "sound_o", "sound_u", "sound_y"
    };
    
    private const float DEFAULT_MIN_PITCH = 1f;
    private const float DEFAULT_MAX_PITCH = 2f;
    private const int DEFAULT_CHARS_TO_SOUND = 3;

    public Array<AudioStreamSample> Sounds { get; } 
        = new Array<AudioStreamSample>();
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
        if (!new File().FileExists("res://" + path))
        {
            LoadDefaultConfig();
            return;
        }

        var configFile = Global.loadJsonFile(path);
        
        if (configFile.Contains("sounds"))
        {
            var sounds = (Dictionary)configFile["sounds"];
            LoadSounds(sounds.Values as Array);
        }

        if (configFile.Contains("min_pitch"))
        {
            MinPitch = Global.ParseFloat(configFile["min_pitch"].ToString());
        }
        
        if (configFile.Contains("max_pitch"))
        {
            MaxPitch = Global.ParseFloat(configFile["max_pitch"].ToString());
        }
        
        if (configFile.Contains("chars_to_sound"))
        {
            CharsToSound = int.Parse(configFile["chars_to_sound"].ToString());
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
            var sound = GD.Load<AudioStreamSample>($"res://assets/audio/dialogues/dynamic/{soundCode}.wav");
            Sounds.Add(sound);
        }
    }
}
