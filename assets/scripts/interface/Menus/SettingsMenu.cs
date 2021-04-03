using Godot;
using Godot.Collections;

public class SettingsMenu : MenuBase
{
    const float MIN_COLOR_SUM = 0.7f;

    Global global = Global.Get();
    AudioStreamPlayer audi;

    private MenuBase otherMenu;

    private Control settingsSubmenu;
    private Control controlsSubmenu;
    
    private Label pageLabel;
    private Button backButton;
    private Label languageLabel;
    private Button languageButton;
    private Label mouseLabel;
    private Label distanceLabel;
    private Label shadowsLabel;
    private Button shadowsButton;
    private Label fullscreenLabel;
    private Label soundLabel;
    private Label musicLabel;
    private Button controlsButton;
    private Slider mouseSlider;
    private Slider distanceSlider;
    private Button fullscreenButton;
    private Slider soundSlider;
    private Slider musicSlider;
    private Button defaultButton;
    private Dictionary<string, Label> controlLabels;

    private Label colorLabel;
    private Slider rSlider;
    private Slider gSlider;
    private Slider bSlider;

    private Label tempEdit;
    private ColorRect tempEditBack;
    private string tempAction = "";
    private bool colorChanged = false;

    private void loadMenu()
    {
        settingsSubmenu = GetNode<Control>("Settings");
        controlsSubmenu = GetNode<Control>("Controls");

        pageLabel = GetNode<Label>("page_label");
        backButton = GetNode<Button>("back");
        languageLabel = GetNode<Label>("Settings/language");
        languageButton = GetNode<Button>("Settings/language_button");
        mouseLabel = GetNode<Label>("Settings/mouse_label");
        distanceLabel = GetNode<Label>("Settings/distance_label");
        shadowsLabel = GetNode<Label>("Settings/shadows_label");
        shadowsButton = GetNode<Button>("Settings/shadows_button");
        fullscreenLabel = GetNode<Label>("Settings/fullscreen");
        fullscreenButton = GetNode<Button>("Settings/fullscreen_button");
        soundLabel = GetNode<Label>("Settings/sound_label");
        musicLabel = GetNode<Label>("Settings/music_label");
        controlsButton = GetNode<Button>("Settings/controls");
        mouseSlider = GetNode<Slider>("Settings/mouse_slider");
        distanceSlider = GetNode<Slider>("Settings/distance_slider");
        soundSlider = GetNode<Slider>("Settings/sound_slider");
        musicSlider = GetNode<Slider>("Settings/music_slider");

        colorLabel = GetNode<Label>("Settings/colorBlock/label");
        rSlider = GetNode<Slider>("Settings/colorBlock/r_slider");
        gSlider = GetNode<Slider>("Settings/colorBlock/g_slider");
        bSlider = GetNode<Slider>("Settings/colorBlock/b_slider");

        defaultButton = GetNode<Button>("Controls/default");
        controlLabels = new Dictionary<string, Label>()
        {
            {"forward", GetNode<Label>("Controls/keyForwardLabel")},
            {"back", GetNode<Label>("Controls/keyBackLabel")},
            {"left", GetNode<Label>("Controls/keyLeftLabel")},
            {"right", GetNode<Label>("Controls/keyRightLabel")},
            {"jump", GetNode<Label>("Controls/keyJumpLabel")},
            {"run", GetNode<Label>("Controls/keyRunLabel")},
            {"use", GetNode<Label>("Controls/keyUseLabel")},
            {"sit", GetNode<Label>("Controls/keyCrouchLabel")},
            {"dash", GetNode<Label>("Controls/keyDashLabel")},
            {"choseHit", GetNode<Label>("Controls/keyLegsLabel")},
            {"changeView", GetNode<Label>("Controls/keyCameraLabel")},
            {"seeTasks", GetNode<Label>("Controls/keyTaskLabel")},
            {"inventory", GetNode<Label>("Controls/keyInventoryLabel")},
            {"autoheal", GetNode<Label>("Controls/keyAutohealLabel")},
            {"quicksave", GetNode<Label>("Controls/keyQuicksaveLabel")},
            {"quickload", GetNode<Label>("Controls/keyQuickloadLabel")},
        };
    }

    
    public override void loadInterfaceLanguage()
    {
        string tempPage = InterfaceLang.GetPhrase("settingsMenu", "pages", otherMenu.menuName);
        pageLabel.Text = tempPage;
        backButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "back");
        languageLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "language");
        languageButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "language");
        mouseLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "mouse");
        distanceLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "distance");
        shadowsLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "shadows");
        fullscreenLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "fullscreen");
        soundLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "sound");
        musicLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "music");
        controlsButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "controls");

        shadowsButton.Text = InterfaceLang.GetPhrase("settingsMenu", "shadows", 
            global.Settings.shadows.ToString());
        
        defaultButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "default");
        foreach(string key in controlLabels.Keys) {
            controlLabels[key].Text = InterfaceLang.GetPhrase("settingsMenu", "controlLabels", key);
        }

        colorLabel.Text = InterfaceLang.GetPhrase("settingsMenu", "labels", "color");
        Color tempColor = global.Settings.interfaceColor;
        rSlider.Value = tempColor.r;
        gSlider.Value = tempColor.g;
        bSlider.Value = tempColor.b;

        loadOnOffText(fullscreenButton, global.Settings.fullscreen);
        loadSliders();
        loadControlButtons();
    }

    private Label getControlEdit(string action) 
    {
        switch(action) {
            case "ui_up": return GetNode<Label>("Controls/forwardBack/edit");
            case "ui_down": return GetNode<Label>("Controls/backBack/edit");
            case "ui_left": return GetNode<Label>("Controls/leftBack/edit");
            case "ui_right": return GetNode<Label>("Controls/rightBack/edit");
            case "jump": return GetNode<Label>("Controls/jumpBack/edit");
            case "ui_shift": return GetNode<Label>("Controls/runBack/edit");
            case "use": return GetNode<Label>("Controls/useBack/edit");
            case "crouch": return GetNode<Label>("Controls/crouchBack/edit");
            case "dash": return GetNode<Label>("Controls/dashBack/edit");
            case "legsHit": return GetNode<Label>("Controls/legsBack/edit");
            case "changeView": return GetNode<Label>("Controls/cameraBack/edit");
            case "task": return GetNode<Label>("Controls/taskBack/edit");
            case "inventory": return GetNode<Label>("Controls/inventoryBack/edit");
            case "autoheal": return GetNode<Label>("Controls/autohealBack/edit");
            case "ui_quicksave": return GetNode<Label>("Controls/quicksaveBack/edit");
            case "ui_quickload": return GetNode<Label>("Controls/quickloadBack/edit");
        }
        return null;
    }

    private void loadSliders()
    {
        mouseSlider.Value = global.Settings.mouseSensivity;
        distanceSlider.Value = global.Settings.distance;
        soundSlider.Value = global.Settings.soundVolume;
        musicSlider.Value = global.Settings.musicVolume;
    }

    private void loadControlButtons()
    {
        foreach(string action in global.Settings.controlActions) {
            var actions = InputMap.GetActionList(action);
            var tempAction = actions[0] as InputEventKey;
            var key = OS.GetScancodeString(tempAction.Scancode);
            var edit = getControlEdit(action);
            WriteKeyToEdit(key, edit);
        }
    }

    private int increase(int value, int max)
    {
        if (value < max) {
            return value + 1;
        } else {
            return 0;
        }
    }

    private void setEditOn(ColorRect editBack, bool on)
    {
        var editButton = editBack.GetNode<Label>("edit");
        if (on) {
            Color tempColor = editBack.Color;
            tempColor.a = 1;
            editBack.Color = tempColor;

            editButton.Modulate = Colors.Black;
            tempEditBack = editBack;
        } else {
            Color tempColor = editBack.Color;
            tempColor.a = 0;
            editBack.Color = tempColor;

            editButton.Modulate = Colors.White;
            tempEditBack = null;
        }
    }

    private void WriteKeyToEdit(string key, Label edit)
    {
        edit.Text = "["  + key.Capitalize() + "]";
    }

    private void cancelControlEdit()
    {
        if (tempAction != "") {
            setEditOn(tempEditBack, false);
            var key = Global.GetKeyName(tempAction);
            if (tempEdit != null) {
                WriteKeyToEdit(key, tempEdit);
            }
            tempAction = "";
            tempEdit = null;
        }
    }

    private void UpdateInterfaceColor()
    {  
        if (!Visible) {
            return;
        }

        LoadColorForChildren(this);
        colorChanged = true;
    }

    public override void SoundClick()
    {
        audi.Play();
    }


    public void OpenMenu(MenuBase self, string menuName)
    {
        otherMenu = self;
        loadInterfaceLanguage();
        this.Visible = true;
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer>("audi");
        base._Ready();
        menuName = "settingsMenu";
        loadMenu();
    }

    public void _on_back_pressed()
    {
        otherMenu.SoundClick();

        if (controlsSubmenu.Visible) {
            controlsSubmenu.Visible = false;
            settingsSubmenu.Visible = true;
        } else {
            this.Visible = false;
            _on_mouse_exited();
            global.Settings.SaveSettings();
            if (colorChanged) {
                ReloadAllColors(GetTree());
            }
        }
    }

    public void _on_language_button_pressed()
    {
        otherMenu.SoundClick();
        InterfaceLang.SetNextLanguage();
        loadInterfaceLanguage();
        otherMenu.loadInterfaceLanguage();
        ReloadMouseEntered();
    }

    private void loadOnOffText(Button button, bool on)
    {
        if (on) {
            button.Text = InterfaceLang.GetPhrase("settingsMenu", "buttonOn", "on");
        } else {
            button.Text = InterfaceLang.GetPhrase("settingsMenu", "buttonOn", "off");
        }
    }

    public void _on_mouse_slider_value_changed(float value)
    {
        global.Settings.mouseSensivity = value;
        if (global.player != null) {
            global.player.MouseSensivity = value;
        } 
    }

    public void _on_distance_slider_value_changed(float value) 
    {
        global.Settings.distance = value;
        if (global.player != null) {
            global.player.RotationHelperThird.thirdCamera.Far = value;
            global.player.RotationHelperThird.firstCamera.Far = value;
        }
    }

    public void _on_shadows_button_pressed()
    {
        otherMenu.SoundClick();
        int tempShadows = global.Settings.shadows;
        tempShadows = increase(tempShadows, global.Settings.shadowVariantsCount);
        global.Settings.ChangeShadows(tempShadows);
        shadowsButton.Text = InterfaceLang.GetPhrase("settingsMenu", "shadows", tempShadows.ToString());
    }

    public void _on_fullscreen_button_pressed()
    {
        otherMenu.SoundClick();
        bool fullscreen = global.Settings.fullscreen;
        global.Settings.SetFullscreen(!fullscreen);
        loadOnOffText(fullscreenButton, !fullscreen);
    }

    public void _on_sound_slider_value_changed(float value)
    {
        global.Settings.SetSoundVolume(value);
    }

    public void _on_music_slider_value_changed(float value)
    {
        global.Settings.SetMusicVolume(value);
    }

    public void _on_controls_pressed()
    {
        otherMenu.SoundClick();
        settingsSubmenu.Visible = false;
        controlsSubmenu.Visible = true;
    }

    public void _on_controls_mouse_entered(string editName, string section, string phrase)
    {
        if (tempEdit == null) {
            base._on_mouse_entered(section, phrase);
            var editBack = GetNode<ColorRect>("Controls/" + editName);
            setEditOn(editBack, true);
        }
    }

    public void _on_controls_mouse_exited()
    {
        if (tempEdit == null && tempEditBack != null) {
            base._on_mouse_exited();
            setEditOn(tempEditBack, false);
        }
    }

    public void _on_controls_gui_input(InputEvent @event, string action)
    {
        if (tempEdit == null) {
            if (@event is InputEventMouseButton) {
                var mouseEv = @event as InputEventMouseButton;
                if (mouseEv.Pressed) {
                    cancelControlEdit();
                    tempEdit = tempEditBack.GetNode<Label>("edit");
                    tempEdit.Text = "[            ]";
                    tempAction = action;
                }
            }
        }
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
        if (colorSum < MIN_COLOR_SUM) {
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
        if (!checkDarkColor(value, color)) {
            return;
        }
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
        UpdateInterfaceColor();
    }
   

    public void _on_default_pressed()
    {
        InputMap.LoadFromGlobals();
        foreach(string action in global.Settings.controlActions) {
            var actions = InputMap.GetActionList(action);
            var tempAction = actions[0] as InputEventKey;
            var key = OS.GetScancodeString(tempAction.Scancode);
            var edit = getControlEdit(action);
            WriteKeyToEdit(key, edit);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (tempEdit != null) {
            if (@event is InputEventKey) {
                var eventKey = @event as InputEventKey;
                if (eventKey.Pressed) {
                    if (eventKey.Scancode == (uint)KeyList.Escape) {
                        cancelControlEdit();
                    } else {
                        InputMap.ActionEraseEvents(tempAction);
                        InputMap.ActionAddEvent(tempAction, eventKey);
                        var key = OS.GetScancodeString(eventKey.Scancode);
                        WriteKeyToEdit(key, tempEdit);
                        tempAction = "";
                        setEditOn(tempEditBack, false);
                        tempEdit = null;
                    }
                }
            }
        }
    }
}
