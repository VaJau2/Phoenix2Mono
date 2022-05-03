using Godot;
using Godot.Collections;

//Скрипт меню настроек
//Отвечает за переключение между подменюшками настроек
//И их взаимодействие
public class SettingsMenu : MenuBase
{
    [Export] private string mainSubmenuCode = "Settings";
    [Export] private Dictionary<string, NodePath> submenuPaths;

    Global global = Global.Get();
    AudioStreamPlayer audi;

    private MenuBase otherMenu;

    private Dictionary<string, SubmenuBase> submenus;

    private Label pageLabel;
    private Button backButton;
    
    private bool colorChanged;
    private bool submenuOpened;

    private void LoadMenu()
    {
        submenus = new Dictionary<string, SubmenuBase>();
        
        foreach (var key in submenuPaths.Keys)
        {
            var path = submenuPaths[key];
            submenus.Add(key, GetNode<Control>(path) as SubmenuBase);
        }

        foreach (var submenu in submenus.Values)
        {
            submenu.LoadSubmenu(this);
        }

        pageLabel = GetNode<Label>("page_label");
        backButton = GetNode<Button>("back");
    }
    
    public override void loadInterfaceLanguage()
    {
        string tempPage = InterfaceLang.GetPhrase("settingsMenu", "pages", otherMenu.menuName);
        pageLabel.Text = tempPage;
        backButton.Text = InterfaceLang.GetPhrase("settingsMenu", "buttons", "back");

        foreach (var submenu in submenus.Values)
        {
            submenu.LoadInterfaceLanguage();
        }
    }
    
    public static int IncreaseInt(int value, int max)
    {
        if (value < max) {
            return value + 1;
        }
        
        return 0;
    }

    public void UpdateInterfaceColor()
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

    public void OpenMenu(MenuBase self)
    {
        otherMenu = self;
        loadInterfaceLanguage();
        Visible = true;
    }

    public void CloseMenu()
    {
        Visible = false;
        _on_mouse_exited();
        global.Settings.SaveSettings();
        if (colorChanged) {
            ReloadAllColors(GetTree());
        }
    }
    
    public static void LoadOnOffText(Button button, bool on)
    {
        button.Text = InterfaceLang.GetPhrase("settingsMenu", "buttonOn", @on ? "on" : "off");
    }

    public void ChangeMenuLanguage()
    {
        otherMenu.SoundClick();
        InterfaceLang.SetNextLanguage();
        loadInterfaceLanguage();
        otherMenu.loadInterfaceLanguage();
        ReloadMouseEntered();
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer>("audi");
        base._Ready();
        menuName = "settingsMenu";
        LoadMenu();
    }
    
    public void _on_submenu_pressed(string submenuCode)
    {
        otherMenu.SoundClick();
        submenuOpened = true;
        
        foreach (var tempSubmenu in submenus.Keys)
        {
            submenus[tempSubmenu].Visible = tempSubmenu == submenuCode;
        }
    }
    
    public void _on_back_pressed()
    {
        otherMenu.SoundClick();

        if (submenuOpened) 
        {
            foreach (var controlsSubmenu in submenus.Values)
            {
                controlsSubmenu.Visible = false;
            }

            submenus[mainSubmenuCode].Visible = true;
            submenuOpened = false;
        } 
        else
        {
            CloseMenu();
        }
    }
}
