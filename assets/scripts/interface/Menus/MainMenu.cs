using Godot;

public class MainMenu : MenuBase
{
    Global global = Global.Get();
    AudioStreamPlayer audi;

    Control changeRaceMenu;
    Control loadMenu;
    Control aboutMenu;

    Control chooseLanguage;
    Control label5;
    Label pageLabel;
    Label labelBorder1;
    Label labelName;
    Label labelBorder2;

    Button continueButton;
    Button startButton;
    Button loadButton;
    Button settingsButton;
    Button aboutButton;
    Button exitButton;

    Label aboutPage;
    Label aboutLabel;
    Button aboutBack;

    Label racePage;
    Button raceBack;
    Button[] raceButtons;
    Label[] raceLabels;

    SettingsMenu settingsMenu;

    [Signal]
    public delegate void labelChanged();

    private void LoadMenu()
    {
        chooseLanguage = GetNode<Control>("chooseLanguage");
        label5 = GetNode<Control>("Label5");
        pageLabel = GetNode<Label>("page_label");
        labelBorder1 = GetNode<Label>("Label2");
        labelName = GetNode<Label>("Label");
        labelBorder2 = GetNode<Label>("Label3");

        continueButton = GetNode<Button>("continue");
        startButton = GetNode<Button>("start");
        loadButton = GetNode<Button>("load");
        settingsButton = GetNode<Button>("settings");
        aboutButton = GetNode<Button>("about");
        exitButton = GetNode<Button>("exit");

        aboutMenu = GetNode<Control>("About");
        aboutPage = GetNode<Label>("About/page_label");
        aboutLabel = GetNode<Label>("About/about_label");
        aboutBack = GetNode<Button>("About/back");

        changeRaceMenu = GetNode<Control>("ChangeRace");
        racePage = GetNode<Label>("ChangeRace/page_label");
        raceBack = GetNode<Button>("ChangeRace/back");
        raceButtons = new Button[3] {
            GetNode<Button>("ChangeRace/earthpony/choose"),
            GetNode<Button>("ChangeRace/unicorn/choose"),
            GetNode<Button>("ChangeRace/pegasus/choose")
        };
        raceLabels = new Label[3] {
            GetNode<Label>("ChangeRace/earthpony/Label"),
            GetNode<Label>("ChangeRace/unicorn/Label"),
            GetNode<Label>("ChangeRace/pegasus/Label")
        };
        loadMenu = GetNode<Control>("Load");

        settingsMenu = GetNode<SettingsMenu>("../SettingsMenu");
    }

    private string getMenuText(string phrase, string section = "main") {
        return InterfaceLang.GetLang(menuName, section, phrase);
    }

    private async void changeLabel(Label label) 
    {
        label.PercentVisible = 0;
        while(label.PercentVisible < 1) 
        {
            label.PercentVisible += 0.1f;
            await global.ToTimer(0.05f, this);
        }
        EmitSignal(nameof(labelChanged));
    }

    private void loadInterfaceLanguage()
    {
        continueButton.Text = getMenuText("continue");
        startButton.Text = getMenuText("start");
        loadButton.Text = getMenuText("load");
        settingsButton.Text = getMenuText("settings");
        aboutButton.Text = getMenuText("about");
        exitButton.Text = getMenuText("exit");
    }

    private void loadAboutLanguage()
    {
        aboutPage.Text = getMenuText("page", "about");
        aboutLabel.Text = getMenuText("label", "about");
        aboutBack.Text = getMenuText("back");
    }

    private void loadRaceLanguage()
    {
        racePage.Text = getMenuText("page", "race");
        raceBack.Text = getMenuText("back");

        for(int i = 0; i < raceButtons.Length; i++) 
        {
            raceButtons[i].Text = getMenuText("button" + i.ToString(), "race");
            raceLabels[i].Text = getMenuText("label" + i.ToString(), "race");
        }
    }

    public override void SoundClick()
    {
        audi.Play();
    }

    public async override void SetMenuVisible(bool animating = false)
    {
        Visible = true;
        pageLabel.Visible = true;

        if (animating) 
        {
            downLabel.Visible = false;
            label5.Visible = false;
            pageLabel.Text = getMenuText("loading");
            await global.ToTimer(1, this);

            pageLabel.Text = getMenuText("welcome");
            changeLabel(pageLabel);
            await global.ToTimer(1.5f, this);

            pageLabel.Text = getMenuText("page");
            changeLabel(pageLabel);
            await ToSignal(this, "labelChanged");

            changeLabel(labelBorder1);
            labelBorder1.Visible = true;
            await ToSignal(this, "labelChanged");

            changeLabel(labelName);
            labelName.Visible = true;
            await ToSignal(this, "labelChanged");

            changeLabel(labelBorder2);
            labelBorder2.Visible = true;
            await ToSignal(this, "labelChanged");
        }
        else
        {
            pageLabel.Text = getMenuText("page");
            labelBorder1.Visible = true;
            labelName.Visible = true;
            labelName.Visible = true;
            labelBorder2.Visible = true;
        }

        downLabel.Visible = true;
        label5.Visible = true;

        loadInterfaceLanguage();
        //TODO - добавить проверку на наличие файлов сохранений
        //continueButton.Visible = true;
        startButton.Visible = true;
        loadButton.Visible = true;
        settingsButton.Visible = true;
        aboutButton.Visible = true;
        exitButton.Visible = true;
    }

    public override void _Ready()
    {
        global.LoadSettings(this);
        
        base._Ready();
        audi = GetNode<AudioStreamPlayer>("audi");
        LoadMenu();

        if (global.Settings.SettingsLoaded) 
        {
            SetMenuVisible(global.mainMenuFirstTime);
        } 
        else 
        {
            chooseLanguage.Visible = true;
            downLabel.Visible = true;
            label5.Visible = true;
        }
        global.mainMenuFirstTime = false;
    }

    public void _on_language_pressed(bool english)
    {
        if (english) 
        {
            InterfaceLang.ChangeLanguage(Language.English);
        }
        chooseLanguage.Visible = false;
        global.Settings.SaveSettings();
        SetMenuVisible(true);
    }

    public void _on_start_pressed()
    {
        SoundClick();
        GetNode<LevelsLoader>("/root/Main").LoadLevel(1); return;
        loadRaceLanguage();
        changeRaceMenu.Visible = true;
    }

    public void _on_load_pressed()
    {
        SoundClick();
        loadMenu.Visible = true;
    }

    public void _on_settings_pressed()
    {
        SoundClick();
        settingsMenu.OpenMenu(this, "main");
    }

    public void _on_about_pressed()
    {
        loadAboutLanguage();
        SoundClick();
        aboutMenu.Visible = true;
    }

    public void _on_back_pressed() 
    {
        SoundClick();
        aboutMenu.Visible = false;
        changeRaceMenu.Visible = false;
        loadMenu.Visible = false;
        _on_mouse_exited();
    }

    public async void _on_exit_pressed()
    {
        SoundClick();
        await global.ToTimer(0.3f, this);
        GetTree().Quit();
    }

    public void _on_choose_pressed(string raceName) 
    {
        Race newRace = Global.raceFromString(raceName);
        global.playerRace = newRace;
        GetNode<LevelsLoader>("/root/Main").LoadLevel(1);
    }
}
