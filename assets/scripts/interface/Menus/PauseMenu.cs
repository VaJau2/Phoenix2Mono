using Godot;

public partial class PauseMenu : MenuBase, IMenu
{
    public bool mustBeClosed => true;
    Global global = Global.Get();
    private MenuAudi audi;
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


    [Signal]
    public delegate void ChangePauseEventHandler(bool value);

    public override void LoadInterfaceLanguage()
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
        SetPause(true);
        Visible = true;
        LoadInterfaceLanguage();
    }

    public void CloseMenu()
    {
        if (!dialogueMenu.MenuOn) 
        {
            SetPause(false);
        } 
        else 
        {
            global.SetPauseMusic(false);
        }
        
        Visible = false;
        settingsMenu.CloseMenu();
        loadMenu.Visible = false;
        saveMenu.Visible = false;
        global.Settings.SaveSettings();
    }

    public override void SoundClick()
    {
        audi.PlayClick();
    }

    protected override void SoundHover()
    {
        audi.PlayHover();
    }

    public override void _Ready()
    {
        audi = GetNode<MenuAudi>("audi");
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

        //на тестовых сценах нельзя сохраняться, т.к. их нет в списке сцен
        saveButton.Disabled = LevelsLoader.tempLevelNum == 0;
    }

    public override void _Input(InputEvent @event)
    {
        if (!Input.IsActionJustPressed("ui_cancel")) return;
        //меню паузы загружается раньше уровня
        if (global.player == null) 
        {
            return;
        }

        if (dialogueMenu == null || !IsInstanceValid(dialogueMenu)) 
        {
            dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        }
            
        if (Visible) 
        {
            loadMenu.Visible = false;
            saveMenu.Visible = false;
            MenuManager.CloseMenu(this);
        } 
        else 
        {
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
        settingsMenu.OpenMenu(this);
    }

    public void _on_exit_pressed()
    {
        MenuManager.ClearMenus();
        SoundClick();
        GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
    }

    private void SetPause(bool value)
    {
        global.SetPause(this, value);
        EmitSignal(SignalName.ChangePause, value);
    }
}
