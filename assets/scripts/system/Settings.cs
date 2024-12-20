using Godot;

public class Settings 
{
    public bool SettingsLoaded;
    public Color interfaceColor = new Color(0.2f, 1f, 0.2f);
    public float mouseSensivity = 0.1f;
    public float distance = 600;
    public int shadows {get; private set;} = 3;
    public int shadowVariantsCount {get;}
    private int[] shadowSettings =
    {
        1024, 2048, 2560, 4096
    };

    public int reflection => reflectionSettings[reflectionIndex];
    private int reflectionIndex = 3;
    private int[] reflectionSettings =
    {
        0, 25, 50, 80
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

    Viewport root;

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
        root.ShadowAtlasSize = tempShadowSetting;
    }

    public void SetFullscreen(bool on)
    {
        fullscreen = on;
        OS.WindowFullscreen = on;
    }

    public Settings(Node node) {
        root = node.GetTree().Root;
        shadowVariantsCount = shadowSettings.Length - 1;
    }

    public void SaveSettings() 
    {
        var config = new ConfigFile();
        config.SetValue("controls", "mouse_sensivity", mouseSensivity);
        foreach(string action in controlActions) {
            var actions = InputMap.GetActionList(action);
            var keyAction = actions[0] as InputEventKey;
            var key = OS.GetScancodeString(keyAction.Scancode);
            config.SetValue("controls", action, key);
        }
        config.SetValue("screen", "distance", distance);
        config.SetValue("screen", "shadows", shadows);
        config.SetValue("screen", "fullscreen", fullscreen);
        config.SetValue("screen", "cameraAngle", cameraAngle);
        config.SetValue("screen", "language", InterfaceLang.GetLang());
        var screenSize = OS.WindowSize;
        config.SetValue("screen", "width", screenSize.x);
        config.SetValue("screen", "height", screenSize.y);
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
        if (err == Error.Ok) {
            mouseSensivity = (float)config.GetValue("controls", "mouse_sensivity");
            foreach(string action in controlActions) {
                string key = config.GetValue("controls", action).ToString();
                var keyCode = (uint)OS.FindScancodeFromString(key);
                var newEvent = new InputEventKey();
                newEvent.Scancode = keyCode;

                InputMap.ActionEraseEvents(action);
                InputMap.ActionAddEvent(action, newEvent);
                
            }
            distance = (float)config.GetValue("screen", "distance");
            shadows = (int)config.GetValue("screen", "shadows");
            ChangeShadows(shadows);
            var tempFullscreen = (bool)config.GetValue("screen", "fullscreen");
            SetFullscreen(tempFullscreen);
            cameraAngle = (bool)config.GetValue("screen", "cameraAngle");
            InterfaceLang.LoadLanguage(config.GetValue("screen", "language").ToString());
            var screenSize = new Vector2();
            screenSize.x = (float)config.GetValue("screen", "width");
            screenSize.y = (float)config.GetValue("screen", "height");
            OS.WindowSize = screenSize;

            interfaceColor = (Color)config.GetValue("screen", "color");

            SetSoundVolume((float)config.GetValue("audio", "sound_volume"));
            SetRadioVolume((float)config.GetValue("audio", "radio_volume"));
            SetMusicVolume((float)config.GetValue("audio", "music_volume"));
            SetVoiceVolume((float)config.GetValue("audio", "voice_volume"));

            playerDamage = (float) config.GetValue("difficulty", "player_damage");
            npcDamage = (float) config.GetValue("difficulty", "npc_damage");
            npcAggressive = (float) config.GetValue("difficulty", "npc_aggressive");
            npcAccuracy = (float) config.GetValue("difficulty", "npc_accuracy");
            inflation = (float) config.GetValue("difficulty", "inflation");

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