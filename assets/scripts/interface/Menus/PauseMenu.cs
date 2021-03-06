using Godot;

public class PauseMenu : MenuBase, IMenu
{
    public bool mustBeClosed {get => true;}
    Global global = Global.Get();
    AudioStreamPlayer audi;
    private InventoryMenu inventoryMenu;
    private DialogueMenu dialogueMenu;
    private Label pageLabel;
    private Label headerLabel;
    private Button continueButton;
    private Button settingsButton;
    private Button exitButton;

    SettingsMenu settingsMenu;

    public override void loadInterfaceLanguage()
    {
        pageLabel.Text      = InterfaceLang.GetPhrase("pauseMenu", "main", "page");
        headerLabel.Text    = InterfaceLang.GetPhrase("pauseMenu", "main", "header");
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
        global.SetPause(this, false);
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
        headerLabel    = GetNode<Label>("Label");
        continueButton = GetNode<Button>("continue");
        settingsButton = GetNode<Button>("settings");
        exitButton     = GetNode<Button>("exit");

        settingsMenu = GetNode<SettingsMenu>("../SettingsMenu");
    }

    private void GetGameMenus() 
    {
        if (inventoryMenu == null) {
            inventoryMenu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        }
        if (dialogueMenu == null) {
            dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_cancel")) {
            GetGameMenus();
            
            if (Visible) {
                MenuManager.CloseMenu(this);
            } else {
                MenuManager.TryToOpenMenu(this, true);
            }

            //если менюшка снимает с паузы, но открыт диалог, возвращаем курсор обратно
            if (!global.paused && dialogueMenu.MenuOn) {
                Input.SetMouseMode(Input.MouseMode.Visible);
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
        SoundClick();
        GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
    }
}
