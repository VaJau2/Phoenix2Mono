using Godot;

//Сабменю настроек графики, интерфейса и громкости
public class SettingsSubmenu : SubmenuBase
{
    const float MIN_COLOR_SUM = 0.7f;
    
    Global global = Global.Get();

    private Label languageLabel;
    private Button languageButton;
    private Label mouseLabel;
    private Slider mouseSlider;
    private Label distanceLabel;
    private Slider distanceSlider;
    private Label shadowsLabel;
    private Button shadowsButton;
    private Label fullscreenLabel;
    private Button fullscreenButton;
    private Label cameraAngleLabel;
    private Button cameraAngleButton;
    private Label soundLabel;
    private Slider soundSlider;
    private Label radioLabel;
    private Slider radioSlider;
    private Label musicLabel;
    private Slider musicSlider;
    private Label voiceLabel;
    private Slider voiceSlider;
    private Button controlsButton;
    private Button difficultyButton;
    
    private Label colorLabel;
    private Slider rSlider;
    private Slider gSlider;
    private Slider bSlider;
    
    public override void LoadSubmenu(SettingsMenu parent)
    {
        base.LoadSubmenu(parent);
        languageLabel = GetNode<Label>("language");
        languageButton = GetNode<Button>("language_button");
        mouseLabel = GetNode<Label>("mouse_label");
        mouseSlider = GetNode<Slider>("mouse_slider");
        distanceLabel = GetNode<Label>("distance_label");
        distanceSlider = GetNode<Slider>("distance_slider");
        shadowsLabel = GetNode<Label>("shadows_label");
        shadowsButton = GetNode<Button>("shadows_button");
        fullscreenLabel = GetNode<Label>("fullscreen");
        fullscreenButton = GetNode<Button>("fullscreen_button");
        cameraAngleLabel = GetNode<Label>("cameraAngle");
        cameraAngleButton = GetNode<Button>("cameraAngle_button");
        soundLabel = GetNode<Label>("sound_label");
        soundSlider = GetNode<Slider>("sound_slider");
        radioLabel = GetNode<Label>("radio_label");
        radioSlider = GetNode<Slider>("radio_slider");
        musicLabel = GetNode<Label>("music_label");
        musicSlider = GetNode<Slider>("music_slider");
        voiceLabel = GetNode<Label>("voice_label");
        voiceSlider = GetNode<Slider>("voice_slider");
        controlsButton = GetNode<Button>("controls");
        difficultyButton = GetNode<Button>("difficulty");
        
        colorLabel = GetNode<Label>("colorBlock/label");
        rSlider = GetNode<Slider>("colorBlock/r_slider");
        gSlider = GetNode<Slider>("colorBlock/g_slider");
        bSlider = GetNode<Slider>("colorBlock/b_slider");
    }

    public override void LoadInterfaceLanguage()
    {
        languageLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "language");
        languageButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "language");
        mouseLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "mouse");
        distanceLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "distance");
        shadowsLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "shadows");
        fullscreenLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "fullscreen");
        cameraAngleLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "cameraAngle");
        soundLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "sound");
        radioLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "radio");
        musicLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "music");
        voiceLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "voice");
        controlsButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "controls");
        difficultyButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "difficulty");

        shadowsButton.Text = InterfaceLang.GetPhrase("settingsMenu", "shadows", 
            global.Settings.shadows.ToString());
        
        colorLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "color");
        Color tempColor = global.Settings.interfaceColor;
        rSlider.Value = tempColor.r;
        gSlider.Value = tempColor.g;
        bSlider.Value = tempColor.b;

        SettingsMenu.LoadOnOffText(cameraAngleButton, global.Settings.cameraAngle);
        SettingsMenu.LoadOnOffText(fullscreenButton, global.Settings.fullscreen);
        
        LoadSliders();

        //временное скрытие громкости озвучки для англ языка
        bool showVoice = InterfaceLang.GetLanguage() == Language.Russian;
        voiceLabel.Visible = showVoice;
        voiceSlider.Visible = showVoice;
    }
    
    private void LoadSliders()
    {
        mouseSlider.Value = global.Settings.mouseSensivity;
        distanceSlider.Value = global.Settings.distance;
        soundSlider.Value = global.Settings.soundVolume;
        radioSlider.Value = global.Settings.radioVolume;
        musicSlider.Value = global.Settings.musicVolume;
        voiceSlider.Value = global.Settings.voiceVolume;
    }
    
    public void _on_language_button_pressed()
    {
        parentMenu.ChangeMenuLanguage();
    }

    public void _on_mouse_slider_value_changed(float value)
    {
        global.Settings.mouseSensivity = value;
        if (IsInstanceValid(global.player)) {
            global.player.MouseSensivity = value;
        } 
    }

    public void _on_distance_slider_value_changed(float value) 
    {
        global.Settings.distance = value;
        if (global.player == null) return;
        global.player.RotationHelperThird.thirdCamera.Far = value;
        global.player.RotationHelperThird.firstCamera.Far = value;
    }

    public void _on_shadows_button_pressed()
    {
        parentMenu.SoundClick();
        int tempShadows = global.Settings.shadows;
        tempShadows = SettingsMenu.IncreaseInt(tempShadows, global.Settings.shadowVariantsCount);
        global.Settings.ChangeShadows(tempShadows);
        shadowsButton.Text = InterfaceLang.GetPhrase("settingsMenu", "shadows", tempShadows.ToString());
    }

    public void _on_fullscreen_button_pressed()
    {
        parentMenu.SoundClick();
        bool fullscreen = global.Settings.fullscreen;
        global.Settings.SetFullscreen(!fullscreen);
        SettingsMenu.LoadOnOffText(fullscreenButton, !fullscreen);
    }
    
    public void _on_cameraAngle_button_pressed()
    {
        parentMenu.SoundClick();
        bool cameraAngle = global.Settings.cameraAngle;
        global.Settings.cameraAngle = !cameraAngle;
        SettingsMenu.LoadOnOffText(cameraAngleButton, !cameraAngle);
    }

    public void _on_sound_slider_value_changed(float value)
    {
        global.Settings.SetSoundVolume(value);
    }

    public void _on_radio_slider_value_changed(float value)
    {
        global.Settings.SetRadioVolume(value);
    }

    public void _on_music_slider_value_changed(float value)
    {
        global.Settings.SetMusicVolume(value);
    }

    public void _on_voice_slider_value_changed(float value)
    {
        global.Settings.SetVoiceVolume(value);
    }
    
    private bool checkDarkColor(float newValue, string color)
    {
        //считаем сумму цветов с учетом нового значения для одного из цветов
        Color tempColor = global.Settings.interfaceColor;
        float tempR = tempColor.r;
        float tempG = tempColor.g;
        float tempB = tempColor.b;

        switch (color)
        {
            case "Red":
                tempR = newValue;
                break;
            case "Green":
                tempG = newValue;
                break;
            case "Blue":
                tempB = newValue;
                break;
        }

        float colorSum = tempR + tempG + tempB;

        //если сумма меньше, возвращаем новое значение обратно
        if (colorSum < MIN_COLOR_SUM) 
        {
            switch(color) 
            {
                case "Red":
                    rSlider.Value = tempColor.r;
                    break;
                case "Green":
                    gSlider.Value = tempColor.g;
                    break;
                case "Blue":
                    bSlider.Value = tempColor.b;
                    break;
            }
        }
        return colorSum >= MIN_COLOR_SUM;
    }

    public void _on_color_slider_value_changed(float value, string color)
    {
        if (!checkDarkColor(value, color)) return;
        
        float tempR = global.Settings.interfaceColor.r;
        float tempG = global.Settings.interfaceColor.g;
        float tempB = global.Settings.interfaceColor.b;

        switch (color)
        {
            case "Red":
                tempR = value;
                break;
            case "Green":
                tempG = value;
                break;
            case "Blue":
                tempB = value;
                break;
        }

        global.Settings.interfaceColor = new Color(
            tempR, tempG, tempB,
            global.Settings.interfaceColor.a
        );
        
        parentMenu.UpdateInterfaceColor();
    }
}
