using Godot;
using Godot.Collections;

public class SettingsMenu : MenuBase
{
    Global global = Global.Get();

    private MenuBase otherMenu;
    
    private Label pageLabel;
    private Label controlsPageLabel;
    private Label headerLabel;
    private Button backButton;
    private Label languageLabel;
    private Button languageButton;
    private Label filterLabel;
    private Button filterButton;
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

    private Control controlsMenu;
    private Label controlsHeader;
    private Button controlsBackButton;
    private Button defaultButton;
    private Dictionary<string, Label> controlLabels;

    private Label tempEdit;
    private ColorRect tempEditBack;
    private string tempAction = "";

    private void loadMenu()
    {
        pageLabel = GetNode<Label>("page_label");
        headerLabel = GetNode<Label>("Label");
        backButton = GetNode<Button>("back");
        languageLabel = GetNode<Label>("language");
        languageButton = GetNode<Button>("language_button");
        filterLabel = GetNode<Label>("filter");
        filterButton = GetNode<Button>("filter_button");
        mouseLabel = GetNode<Label>("mouse_label");
        distanceLabel = GetNode<Label>("distance_label");
        shadowsLabel = GetNode<Label>("shadows_label");
        shadowsButton = GetNode<Button>("shadows_button");
        fullscreenLabel = GetNode<Label>("fullscreen");
        fullscreenButton = GetNode<Button>("fullscreen_button");
        soundLabel = GetNode<Label>("sound_label");
        musicLabel = GetNode<Label>("music_label");
        controlsButton = GetNode<Button>("controls");
        mouseSlider = GetNode<Slider>("mouse_slider");
        distanceSlider = GetNode<Slider>("distance_slider");
        soundSlider = GetNode<Slider>("sound_slider");
        musicSlider = GetNode<Slider>("music_slider");

        controlsPageLabel = GetNode<Label>("Controls/page_label");
        controlsMenu = GetNode<Control>("Controls");
        controlsHeader = GetNode<Label>("Controls/Label");
        controlsBackButton = GetNode<Button>("Controls/back");
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
            {"getGun", GetNode<Label>("Controls/keyGetGunLabel")},
            {"choseHit", GetNode<Label>("Controls/keyLegsLabel")},
            {"changeView", GetNode<Label>("Controls/keyCameraLabel")},
            {"seeTasks", GetNode<Label>("Controls/keyTaskLabel")},
        };
    }

    
    private void loadInterfaceLanguage()
    {
        string tempPage = InterfaceLang.GetLang("settingsMenu", "pages", otherMenu.menuName);
        pageLabel.Text = tempPage;
        headerLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "header");
        backButton.Text = InterfaceLang.GetLang("settingsMenu", "buttons", "back");
        languageLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "language");
        languageButton.Text = InterfaceLang.GetLang("settingsMenu", "buttons", "language");
        filterLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "filter");
        mouseLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "mouse");
        distanceLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "distance");
        shadowsLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "shadows");
        fullscreenLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "fullscreen");
        soundLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "sound");
        musicLabel.Text = InterfaceLang.GetLang("settingsMenu", "labels", "music");
        
        controlsPageLabel.Text = tempPage + InterfaceLang.GetLang("settingsMenu", "pages", "controls");
        controlsHeader.Text = InterfaceLang.GetLang("settingsMenu", "labels", "controlsHeader");
        controlsButton.Text = InterfaceLang.GetLang("settingsMenu", "buttons", "controls");

        shadowsButton.Text = InterfaceLang.GetLang("settingsMenu", "shadows", 
            global.Settings.shadows.ToString());
        
        controlsBackButton.Text = backButton.Text;
        defaultButton.Text = InterfaceLang.GetLang("settingsMenu", "buttons", "default");
        foreach(string key in controlLabels.Keys) {
            controlLabels[key].Text = InterfaceLang.GetLang("settingsMenu", "controlLabels", key);
        }

        loadOnOffText(filterButton, global.Settings.filter);
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
            case "getGun": return GetNode<Label>("Controls/getGunBack/edit");
            case "legsHit": return GetNode<Label>("Controls/legsBack/edit");
            case "changeView": return GetNode<Label>("Controls/cameraBack/edit");
            case "task": return GetNode<Label>("Controls/taskBack/edit");
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
        var spacing = "     ";
        var keyLen = key.Length;
        if (keyLen > 1) {
            var spacintLen = spacing.Length;
            if (keyLen >= spacintLen) {
                spacing = "";
            } else {
                spacing = spacing.Substr(0, (spacintLen - (keyLen) / 2) - 1);
            }
        }
        edit.Text = "[" + spacing + key.Capitalize() + spacing + "]";
    }

    private void cancelControlEdit()
    {
        if (tempAction != "") {
            setEditOn(tempEditBack, false);
            var actions = InputMap.GetActionList(tempAction);
            var action = actions[0] as InputEventKey;
            var key = OS.GetScancodeString(action.Scancode);
            if (tempEdit != null) {
                WriteKeyToEdit(key, tempEdit);
            }
            tempAction = "";
            tempEdit = null;
        }
    }


    public void OpenMenu(MenuBase self, string menuName)
    {
        otherMenu = self;
        loadInterfaceLanguage();
        otherMenu.Visible = false;
        this.Visible = true;
    }

    public override void _Ready()
    {
        base._Ready();
        menuName = "settingsMenu";
        loadMenu();
    }

    public void _on_back_pressed()
    {
        otherMenu.SoundClick();
        this.Visible = false;
        otherMenu.SetMenuVisible(false);
        _on_mouse_exited();
        global.Settings.SaveSettings();
    }

    public void _on_language_button_pressed()
    {
        otherMenu.SoundClick();
        InterfaceLang.SetNextLanguage();
        loadInterfaceLanguage();
        ReloadMouseEntered();
    }

    private void loadOnOffText(Button button, bool on)
    {
        if (on) {
            button.Text = InterfaceLang.GetLang("settingsMenu", "buttonOn", "on");
        } else {
            button.Text = InterfaceLang.GetLang("settingsMenu", "buttonOn", "off");
        }
    }

    public void _on_filter_button_pressed()
    {
        otherMenu.SoundClick();
        bool filter = global.Settings.filter;
        global.Settings.filter = !filter;
        loadOnOffText(filterButton, !filter);
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
        shadowsButton.Text = InterfaceLang.GetLang("settingsMenu", "shadows", tempShadows.ToString());
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
        controlsMenu.Visible = true;
    }

    public void _on_controls_back_pressed() 
    {
        otherMenu.SoundClick();
        controlsMenu.Visible = false;
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