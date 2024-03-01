using Godot;

public partial class Settings 
{
    public bool SettingsLoaded;
    public Color interfaceColor = new(0.2f, 1f, 0.2f);
    public float mouseSensivity = 0.1f;
    public float distance = 600;
    public int shadows {get; private set;} = 3;
    public int shadowVariantsCount {get;}
    private int[] shadowSettings =
    {
        1024, 2048, 2560, 4096
    };

    public float soundVolume {get; private set;}
    public float radioVolume {get; private set;}
    public float musicVolume {get; private set;}
    public float voiceVolume {get; private set;}

    public bool fullscreen {get; private set;}
    public bool cameraAngle = true;

    public string[] controlActions = 
    {
        "ui_up", "ui_down", "ui_left", "ui_right",
        "jump", "ui_shift", "use", "crouch", "dash",
        "legsHit", "changeView", "task", "inventory",
        "autoheal", "ui_quicksave", "ui_quickload"
    };

    public float playerDamage = DifficultySubmenu.DEFAULT_SLIDERS_VALUE;
    public float npcDamage = DifficultySubmenu.DEFAULT_SLIDERS_VALUE;
    public float npcAggressive = DifficultySubmenu.DEFAULT_SLIDERS_VALUE;
    public float npcAccuracy = DifficultySubmenu.DEFAULT_SLIDERS_VALUE;
    public float inflation = DifficultySubmenu.DEFAULT_SLIDERS_VALUE;

    Window root;

    public void UpdateAudioBus(AudioBus bus, float value)
    {
        AudioServer.SetBusVolumeDb((int)bus, value);
        AudioServer.SetBusMute((int)bus, value <= -80);
    }

    public void SetSoundVolume(float volume) 
    {
        UpdateAudioBus(AudioBus.Sound, volume);
        soundVolume = volume;
    }

    public void SetRadioVolume(float volume)
    {
        UpdateAudioBus(AudioBus.Radio, volume);
        radioVolume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        UpdateAudioBus(AudioBus.Music, volume);
        musicVolume = volume;
    }

    public void SetVoiceVolume(float volume)
    {
        UpdateAudioBus(AudioBus.Voice, volume);
        voiceVolume = volume;
    }

    public void ChangeShadows(int settingNum) 
    {
        shadows = settingNum;
        var tempShadowSetting = shadowSettings[settingNum];
        root.PositionalShadowAtlasSize = tempShadowSetting;
    }

    public void SetFullscreen(bool on)
    {
        fullscreen = on;
        DisplayServer.WindowSetMode(
            on
                ? DisplayServer.WindowMode.Fullscreen
                : DisplayServer.WindowMode.Windowed
        );
    }

    public Settings(Node node)
    {
        root = node.GetTree().Root;
        shadowVariantsCount = shadowSettings.Length - 1;
    }

    public void SaveSettings() 
    {
        var config = new ConfigFile();
        config.SetValue("controls", "mouse_sensivity", mouseSensivity);
        foreach(string action in controlActions) {
            var actions = InputMap.ActionGetEvents(action);
            var keyAction = actions[0] as InputEventKey;
            var key = OS.GetKeycodeString(keyAction.Keycode);
            config.SetValue("controls", action, key);
        }
        config.SetValue("screen", "distance", distance);
        config.SetValue("screen", "shadows", shadows);
        config.SetValue("screen", "fullscreen", fullscreen);
        config.SetValue("screen", "cameraAngle", cameraAngle);
        config.SetValue("screen", "language", InterfaceLang.GetLang());
        var screenSize = DisplayServer.WindowGetSize();
        config.SetValue("screen", "width", screenSize.X);
        config.SetValue("screen", "height", screenSize.Y);
        config.SetValue("screen", "color", interfaceColor);

        config.SetValue("audio", "sound_volume", soundVolume);
        config.SetValue("audio", "radio_volume", radioVolume);
        config.SetValue("audio", "music_volume", musicVolume);
        config.SetValue("audio", "voice_volume", voiceVolume);
        
        config.SetValue("difficulty", "player_damage", playerDamage);
        config.SetValue("difficulty", "npc_damage", npcDamage);
        config.SetValue("difficulty", "npc_aggressive", npcAggressive);
        config.SetValue("difficulty", "npc_accuracy", npcAccuracy);
        config.SetValue("difficulty", "inflation", inflation);

        config.Save("res://settings.cfg");
    }

    public void LoadSettings() 
    {
        var config = new ConfigFile();
        var err = config.Load("res://settings.cfg");
        if (err == Error.Ok) 
        {
            mouseSensivity = (float)config.GetValue("controls", "mouse_sensivity");
            foreach (string action in controlActions) 
            {
                string key = config.GetValue("controls", action).ToString();
                var keyCode = OS.FindKeycodeFromString(key);
                var newEvent = new InputEventKey();
                newEvent.Keycode = keyCode;

                InputMap.ActionEraseEvents(action);
                InputMap.ActionAddEvent(action, newEvent);
            }
            
            distance = config.GetValue("screen", "distance").AsSingle();
            shadows = config.GetValue("screen", "shadows").AsInt32();
            ChangeShadows(shadows);
            var tempFullscreen = (bool)config.GetValue("screen", "fullscreen");
            SetFullscreen(tempFullscreen);
            cameraAngle = config.GetValue("screen", "cameraAngle").AsBool();
            InterfaceLang.LoadLanguage(config.GetValue("screen", "language").ToString());
            var screenSize = new Vector2I
            {
                X = config.GetValue("screen", "width").AsInt32(),
                Y = config.GetValue("screen", "height").AsInt32()
            };
            DisplayServer.WindowSetSize(screenSize);

            interfaceColor = config.GetValue("screen", "color").AsColor();

            SetSoundVolume(config.GetValue("audio", "sound_volume").AsSingle());
            SetRadioVolume(config.GetValue("audio", "radio_volume").AsSingle());
            SetMusicVolume(config.GetValue("audio", "music_volume").AsSingle());
            SetVoiceVolume(config.GetValue("audio", "voice_volume").AsSingle());

            playerDamage = config.GetValue("difficulty", "player_damage").AsSingle();
            npcDamage = config.GetValue("difficulty", "npc_damage").AsSingle();
            npcAggressive = config.GetValue("difficulty", "npc_aggressive").AsSingle();
            npcAccuracy = config.GetValue("difficulty", "npc_accuracy").AsSingle();
            inflation = config.GetValue("difficulty", "inflation").AsSingle();

            SettingsLoaded = true;
        }
    }
}

public enum AudioBus
{
    Master,
    Music,
    Sound,
    Voice,
    Radio
}