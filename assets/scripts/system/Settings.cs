using Godot;

public class Settings 
{
    public bool SettingsLoaded = false;
    Viewport root;
    public float mouseSensivity = 0.1f;
    public float distance = 600;
    public int shadows {get; private set;} = 3;
    public int shadowVariantsCount {get; private set;}
    private int[] shadowSettings = new int[] 
    {
        1024, 2048, 2560, 4096
    };
    public int reflections = 3;
    private int[] reflectionSettings = new int[] 
    {
        0, 30, 50, 90
    };
    public int reflectionVariantsCount {get; private set;}

    public float soundVolume {get; private set;}
    public float musicVolume {get; private set;}

    public bool fullscreen {get; private set;} = false;

    public string[] controlActions = new string[] 
    {
        "ui_up", "ui_down", "ui_left", "ui_right",
        "jump", "ui_shift", "use", "crouch", "dash",
        "getGun", "legsHit", "changeView", "task"
    };

    private void updateAudioBus(int num, float value)
    {
        AudioServer.SetBusVolumeDb(num, value);
        AudioServer.SetBusMute(num, (value == -8));
    }

    public void SetSoundVolume(float volume) 
    {
        updateAudioBus(0, volume);
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

    public int GetReflections() 
    {
        return reflectionSettings[reflections];
    }

    public void SetFullscreen(bool on)
    {
        fullscreen = on;
        OS.WindowFullscreen = on;
    }

    public Settings(Node menu) {
        root = menu.GetTree().Root;
        shadowVariantsCount = shadowSettings.Length - 1;
        reflectionVariantsCount = reflectionSettings.Length - 1;
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
        config.SetValue("screen", "reflections", reflections);
        config.SetValue("screen", "fullscreen", fullscreen);
        config.SetValue("screen", "language", InterfaceLang.GetLang());
        var screenSize = OS.WindowSize;
        config.SetValue("screen", "width", screenSize.x);
        config.SetValue("screen", "height", screenSize.y);

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
            reflections = (int)config.GetValue("screen", "reflections");
            var tempFullscreen = (bool)config.GetValue("screen", "fullscreen");
            SetFullscreen(tempFullscreen);
            InterfaceLang.LoadLanguage(config.GetValue("screen", "language").ToString());
            var screenSize = new Vector2();
            screenSize.x = (float)config.GetValue("screen", "width");
            screenSize.y = (float)config.GetValue("screen", "height");
            OS.WindowSize = screenSize;

            SetSoundVolume((float)config.GetValue("audio", "sound_volume"));
            SetMusicVolume((float)config.GetValue("audio", "music_volume"));
            
            SettingsLoaded = true;
        }
    }
}