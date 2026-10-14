using Godot;

public partial class DeathMenu : MenuBase
{
    MenuAudi audi;

    private Control menuPage;
    private LoadMenu loadPage;
    
    private Label pageLabel;
    private Label headerLabel;
    private Button againButton;
    private Button loadButton;
    private Button exitButton;
    
    private Control modalError;
    private Label modalHeader;
    private Label modalDesc;
    private Button modalOk;

    private void loadMenu()
    {
        menuName = "deathMenu";
        
        menuPage = GetNode<Control>("Menu");
        loadPage = GetNode<LoadMenu>("Load");
        
        pageLabel   = GetNode<Label>("Menu/page_label");
        headerLabel = GetNode<Label>("Menu/Label");
        againButton = GetNode<Button>("Menu/again");
        loadButton  = GetNode<Button>("Menu/load");
        exitButton  = GetNode<Button>("Menu/exit");
        
        modalError = GetNode<Control>("modalError");
        modalHeader = modalError.GetNode<Label>("back/Header");
        modalDesc = modalError.GetNode<Label>("back/Text");
        modalOk = modalError.GetNode<Button>("back/OK");
    }

    public override void LoadInterfaceLanguage()
    {
        pageLabel.Text   = InterfaceLang.GetPhrase("deathMenu", "main", "page");
        againButton.Text = InterfaceLang.GetPhrase("deathMenu", "main", "again");
        loadButton.Text  = InterfaceLang.GetPhrase("deathMenu", "main", "load");
        exitButton.Text  = InterfaceLang.GetPhrase("deathMenu", "main", "exit");
        
        modalHeader.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "header");
        modalDesc.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "desc");
        modalOk.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "ok");
        getRandomHeaderPhrase();
    }

    private void getRandomHeaderPhrase()
    {
        var phrases = InterfaceLang.GetPhrasesSection("deathMenu", "header");
        var phrasesCount = phrases.Keys.Count;
        var rand = new RandomNumberGenerator();
        rand.Randomize();
        string randI = rand.RandiRange(0, phrasesCount - 1).ToString();
        headerLabel.Text = phrases[randI].ToString();
    }
    
    public void _on_again_pressed()
    {
        SoundClick();
        string lastSave = LoadMenu.GetLastSaveFile();
        if (!LoadMenu.TryToLoadGame(lastSave, GetNode<LevelsLoader>("/root/Main")))
        {
            modalError.Visible = true;
        }
    }
    public void _on_error_OK_pressed()
    {
        modalError.Visible = false;
    }

    public void _on_load_pressed()
    {
        loadPage.UpdateTable();

        SoundClick();
        menuPage.Visible = false;
        loadPage.Visible = true;
    }

    public void _on_Load_BackPressed()
    {
        SoundClick();
        menuPage.Visible = true;
        loadPage.Visible = false;
    }
    
    public void _on_exit_pressed()
    {
        MenuManager.ClearMenus();
        SoundClick();
        GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
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
        loadMenu();
        LoadInterfaceLanguage();
    }
}
