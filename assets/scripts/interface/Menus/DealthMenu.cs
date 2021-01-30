using Godot;

public class DealthMenu : MenuBase
{
    AudioStreamPlayer audi;

    private Label pageLabel;
    private Label headerLabel;
    private Button againButton;
    private Button loadButton;
    private Button exitButton;

    private void loadMenu()
    {
        pageLabel   = GetNode<Label>("page_label");
        headerLabel = GetNode<Label>("Label");
        againButton = GetNode<Button>("again");
        loadButton  = GetNode<Button>("load");
        exitButton  = GetNode<Button>("exit");
    }

    public override void loadInterfaceLanguage()
    {
        pageLabel.Text   = InterfaceLang.GetPhrase("dealthMenu", "main", "page");
        againButton.Text = InterfaceLang.GetPhrase("dealthMenu", "main", "again");
        loadButton.Text  = InterfaceLang.GetPhrase("dealthMenu", "main", "load");
        exitButton.Text  = InterfaceLang.GetPhrase("dealthMenu", "main", "exit");
        getRandomHeaderPhrase();
    }

    private void getRandomHeaderPhrase()
    {
        var phrases = InterfaceLang.GetPhrasesSection("dealthMenu", "header");
        var phrasesCount = phrases.Keys.Count;
        var rand = new RandomNumberGenerator();
        rand.Randomize();
        string randI = rand.RandiRange(0, phrasesCount - 1).ToString();
        headerLabel.Text = phrases[randI].ToString();
    }

    public void _on_again_pressed()
    {
        SoundClick();
        GetNode<LevelsLoader>("/root/Main").ReloadLevel();
    }

    public void _on_exit_pressed()
    {
        SoundClick();
        GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
    }

    public override void SoundClick()
    {
        audi.Play();
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer>("audi");
        base._Ready();
        menuName = "dealthMenu";
        loadMenu();
        loadInterfaceLanguage();
    }
}
