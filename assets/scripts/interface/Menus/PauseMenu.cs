using Godot;

public class PauseMenu : MenuBase, IMenu
{
    public bool mustBeClosed {get => true;}
    Global global = Global.Get();
    AudioStreamPlayer audi;
    private DialogueMenu dialogueMenu;
    private Label pageLabel;
    private Button continueButton;
    private Button saveButton;
    private Button loadButton;
    private Button settingsButton;
    private Button exitButton;

    private LoadMenu loadMenu;
    private SaveMenu saveMenu;
    
    private SettingsMenu settingsMenu;

    public override void loadInterfaceLanguage()
    {
        pageLabel.Text      = InterfaceLang.GetPhrase("pauseMenu", "main", "page");
        continueButton.Text = InterfaceLang.GetPhrase("pauseMenu", "main", "continue");
        saveButton.Text     = InterfaceLang.GetPhrase("pauseMenu", "main", "save");
        loadButton.Text     = InterfaceLang.GetPhrase("pauseMenu", "main", "load");
        settingsButton.Text = InterfaceLang.GetPhrase("pauseMenu", "main", "settings");
        exitButton.Text     = InterfaceLang.GetPhrase("pauseMenu", "main", "exit");
        saveMenu.LoadInterfaceLanguage();
        loadMenu.LoadInterfaceLanguage();
    }

    public void OpenMenu()
    {
        global.SetPause(this, true);
        this.Visible = true;
        loadInterfaceLanguage();
    }

    public void CloseMenu()
    {
        if (!dialogueMenu.MenuOn) {
            global.SetPause(this, false);
        } else {
            global.SetPauseMusic(false);
        }
        
        this.Visible = false;
        settingsMenu.Visible = false;
        loadMenu.Visible = false;
        saveMenu.Visible = false;
        global.Settings.SaveSettings();
    }

    public override void SoundClick()
    {
        audi.Play();
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer>("audi");
        base._Ready();
        menuName = "pauseMenu";
        
        pageLabel      = GetNode<Label>("page_label");
        continueButton = GetNode<Button>("continue");
        saveButton     = GetNode<Button>("save");
        loadButton     = GetNode<Button>("load");
        settingsButton = GetNode<Button>("settings");
        exitButton     = GetNode<Button>("exit");

        settingsMenu = GetNode<SettingsMenu>("../SettingsMenu");
        loadMenu = GetNode<LoadMenu>("Load");
        saveMenu = GetNode<SaveMenu>("Save");
    }

    public override void _Input(InputEvent @event)
    {
        if (!Input.IsActionJustPressed("ui_cancel")) return;
        //меню паузы загружается раньше уровня
        if (global.player == null) {
            return;
        }

        if (dialogueMenu == null) {
            dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        }
            
        if (Visible) {
            loadMenu.Visible = false;
            saveMenu.Visible = false;
            MenuManager.CloseMenu(this);
        } else {
            MenuManager.TryToOpenMenu(this, true);
        }
    }
    
    public void _on_back_Pressed()
    {
        SoundClick();
        loadMenu.Visible = false;
        saveMenu.Visible = false;
    }


    public void _on_continue_pressed() 
    {
        SoundClick();
        MenuManager.CloseMenu(this);
    }

    public void _on_save_pressed()
    {
        SoundClick();
        saveMenu.UpdateTable();
        saveMenu.Visible = true;
    }

    public void _on_load_pressed()
    {
        SoundClick();
        loadMenu.UpdateTable();
        loadMenu.Visible = true;
    }
    
    public void _on_settings_pressed()
    {
        SoundClick();
        settingsMenu.OpenMenu(this, "main");
    }

    public void _on_exit_pressed()
    {
        MenuManager.ClearMenus();
        SoundClick();
        GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
    }
}
