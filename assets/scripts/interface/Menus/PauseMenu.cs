using Godot;

public class PauseMenu : MenuBase, IMenu
{
    public bool mustBeClosed {get => true;}
    Global global = Global.Get();
    AudioStreamPlayer audi;
    private DialogueMenu dialogueMenu;
    private Label pageLabel;
    private Button continueButton;
    private Button settingsButton;
    private Button exitButton;

    SettingsMenu settingsMenu;

    public override void loadInterfaceLanguage()
    {
        pageLabel.Text      = InterfaceLang.GetPhrase("pauseMenu", "main", "page");
        continueButton.Text = InterfaceLang.GetPhrase("pauseMenu", "main", "continue");
        settingsButton.Text = InterfaceLang.GetPhrase("pauseMenu", "main", "settings");
        exitButton.Text     = InterfaceLang.GetPhrase("pauseMenu", "main", "exit");
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
        settingsButton = GetNode<Button>("settings");
        exitButton     = GetNode<Button>("exit");

        settingsMenu = GetNode<SettingsMenu>("../SettingsMenu");
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_cancel")) {
            //меню паузы загружается раньше уровня
            if (global.player == null) {
                return;
            }

            if (dialogueMenu == null) {
                dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
            }
            
            if (Visible) {
                MenuManager.CloseMenu(this);
            } else {
                MenuManager.TryToOpenMenu(this, true);
            }
        }
    }

    public void _on_continue_pressed() 
    {
        SoundClick();
        MenuManager.CloseMenu(this);
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
