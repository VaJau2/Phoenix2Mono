using Godot;

public class Settings 
{
    public bool SettingsLoaded = false;
    Viewport root;
    public Color interfaceColor = new Color(0.226f, 0.75f, 0.144f);
    public float mouseSensivity = 0.1f;
    public float distance = 600;
    public int shadows {get; private set;} = 3;
    public int shadowVariantsCount {get; private set;}
    private int[] shadowSettings = new int[] 
    {
        1024, 2048, 2560, 4096
    };

    public float soundVolume {get; private set;}
    public float musicVolume {get; private set;}

    public bool fullscreen {get; private set;} = false;

    public string[] controlActions = new string[] 
    {
        "ui_up", "ui_down", "ui_left", "ui_right",
        "jump", "ui_shift", "use", "crouch", "dash",
        "legsHit", "changeView", "task", "inventory",
        "autoheal", "ui_quicksave", "ui_quickload"
    };

    private void updateAudioBus(int num, float value)
    {
        AudioServer.SetBusVolumeDb(num, value);
        AudioServer.SetBusMute(num, (value == -8));
    }

    public void SetSoundVolume(float volume) 
    {
        updateAudioBus(2, volume);
        soundVolume = volume;
    }

    public void SetMusicVolume(float volume)
    {
        updateAudioBus(1, volume);
        musicVolume = volume;
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

    public Settings(Node menu) {
        root = menu.GetTree().Root;
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
        config.SetValue("screen", "language", InterfaceLang.GetLang());
        var screenSize = OS.WindowSize;
        config.SetValue("screen", "width", screenSize.x);
        config.SetValue("screen", "height", screenSize.y);
        config.SetValue("screen", "color", interfaceColor);

        config.SetValue("audio", "sound_volume", soundVolume);
        config.SetValue("audio", "music_volume", musicVolume);
        
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
            InterfaceLang.LoadLanguage(config.GetValue("screen", "language").ToString());
            var screenSize = new Vector2();
            screenSize.x = (float)config.GetValue("screen", "width");
            screenSize.y = (float)config.GetValue("screen", "height");
            OS.WindowSize = screenSize;

            interfaceColor = (Color)config.GetValue("screen", "color");

            SetSoundVolume((float)config.GetValue("audio", "sound_volume"));
            SetMusicVolume((float)config.GetValue("audio", "music_volume"));
            
            SettingsLoaded = true;
        }
    }
}