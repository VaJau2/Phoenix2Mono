using Godot;

public class PauseMenu : MenuBase
{
    Global global = Global.Get();
    AudioStreamPlayer audi;
    private InventoryMenu inventoryMenu;
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

    private void setPause(bool pause) 
    {
        global.SetPause(this, pause);
        this.Visible = pause;
        if (pause) {
            loadInterfaceLanguage();
        } else {
            settingsMenu.Visible = false;
            global.Settings.SaveSettings();
        }
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

    private void GetInventory() 
    {
        if (inventoryMenu == null) {
            inventoryMenu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("ui_cancel")) {
            GetInventory();
            
            if (inventoryMenu.isOpen) {
                inventoryMenu.CloseMenu();
            } else if (global.player.Health > 0) {
                setPause(!global.paused);
            }
        }
    }

    public void _on_continue_pressed() 
    {
        SoundClick();
        setPause(false);
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
