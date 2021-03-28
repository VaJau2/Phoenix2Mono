using Godot;

public class LoadMenu : Control
{
    [Signal]
    public delegate void BackPressed();
    
    private MenuBase parentMenu;
    
    private Label pageLabel;

    private Button backButton;
    private Control tableBack;
    private Table table;
    
    private Button loadButton;
    private Button deleteButton;
    
    public void UpdateTable()
    {
        table.RedrawAllButtons();
    }

    public void LoadInterfaceLanguage()
    {
        var menuName = parentMenu is MainMenu ? "main" : "pause";
        pageLabel.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "page_load_" + menuName);
        backButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "back");
        loadButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "load");
        deleteButton.Text = InterfaceLang.GetPhrase("saveloadMenu", "main", "delete");
        
        table.HeaderName = InterfaceLang.GetPhrase("saveloadMenu", "table", "name");
        table.HeaderDate = InterfaceLang.GetPhrase("saveloadMenu", "table", "date");
        table.HeaderLevel = InterfaceLang.GetPhrase("saveloadMenu", "table", "level");
        table.UpdateHeader();
    }

    public override void _Ready()
    {
        parentMenu = GetParent<MenuBase>();

        pageLabel = GetNode<Label>("page_label");
        backButton = GetNode<Button>("back");
        tableBack = GetNode<Control>("Table");
        table = tableBack.GetNode<Table>("table");
        loadButton = GetNode<Button>("Load");
        deleteButton = GetNode<Button>("Delete");
        LoadInterfaceLanguage();
    }

    public void _on_back_pressed()
    {
        parentMenu.SoundClick();
        EmitSignal(nameof(BackPressed));
    }

    public void _on_mouse_entered(string section, string messageLink)
    {
        parentMenu._on_mouse_entered(section, messageLink, "saveloadMenu");
    }

    public void _on_mouse_exited()
    {
        parentMenu._on_mouse_exited();
    }
    
    public void _on_TableButtonPressed(string fileName)
    {
        parentMenu.SoundClick();
        loadButton.Disabled = false;
        deleteButton.Disabled = false;
    }

    public void _on_Delete_pressed()
    {
        parentMenu.SoundClick();
        string saveName = table.GetTempFileData.name;
        string fileName = SaveMenu.GetLikeLatinString(saveName)+ ".sav";
        
        Global.DeleteSaveFile(fileName);
        table.DeleteButton(saveName);
        
        loadButton.Disabled = true;
        deleteButton.Disabled = true;
    }
}
