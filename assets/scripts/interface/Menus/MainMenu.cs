using System;
using Godot;

public partial class MainMenu : MenuBase
{
    Global global = Global.Get();
    MenuAudi audi;
    AudioStreamPlayer music;

    ColorRect backgroundRect;

    Control autosaveMenu;
    Control changeRaceMenu;
    LoadMenu loadMenu;
    TestingLevelsMenu testingLevelsMenu;
    Control aboutMenu;

    Control chooseLanguage;
    Control label5;
    Label pageLabel;

    Button continueButton;
    Button startButton;
    Button loadButton;
    Button settingsButton;
    Button aboutButton;
    Button exitButton;
    Button testingButton;

    Label aboutPage;
    Label aboutLabel;
    Button aboutBack;

    Label autosavePage;
    Label autosaveHeader;
    LineEdit autosaveInput;
    Button autosaveNext;
    Button autosaveBack;

    Label racePage;
    Button raceBack;
    Label[] raceButtons;
    Label[] raceLabels;
    Control tempHover;

    SettingsMenu settingsMenu;
    
    private Control modalError;
    private Label modalHeader;
    private Label modalDesc;
    private Button modalOk;

    [Signal]
    public delegate void LabelChangedEventHandler();

    private void LoadMenu()
    {
        backgroundRect = GetNode<ColorRect>("background");
        chooseLanguage = GetNode<Control>("chooseLanguage");
        label5 = GetNode<Control>("Label5");
        pageLabel = GetNode<Label>("page_label");

        continueButton = GetNode<Button>("continue");
        startButton = GetNode<Button>("start");
        loadButton = GetNode<Button>("load");
        settingsButton = GetNode<Button>("settings");
        aboutButton = GetNode<Button>("about");
        exitButton = GetNode<Button>("exit");
        testingButton = GetNode<Button>("testing");

        aboutMenu = GetNode<Control>("About");
        aboutPage = GetNode<Label>("About/page_label");
        aboutLabel = GetNode<Label>("About/about_label");
        aboutBack = GetNode<Button>("About/back");

        autosaveMenu = GetNode<Control>("ChooseAutosaveName");
        autosavePage = autosaveMenu.GetNode<Label>("page_label");
        autosaveBack = autosaveMenu.GetNode<Button>("back");
        autosaveHeader = autosaveMenu.GetNode<Label>("header");
        autosaveInput = autosaveMenu.GetNode<LineEdit>("input");
        autosaveNext = autosaveMenu.GetNode<Button>("next");

        changeRaceMenu = GetNode<Control>("ChangeRace");
        racePage = GetNode<Label>("ChangeRace/page_label");
        raceBack = GetNode<Button>("ChangeRace/back");
        raceButtons = new Label[3] {
            GetNode<Label>("ChangeRace/earthpony/choose"),
            GetNode<Label>("ChangeRace/unicorn/choose"),
            GetNode<Label>("ChangeRace/pegasus/choose")
        };
        raceLabels = new Label[3] {
            GetNode<Label>("ChangeRace/earthpony/Label"),
            GetNode<Label>("ChangeRace/unicorn/Label"),
            GetNode<Label>("ChangeRace/pegasus/Label")
        };
        
        modalError = GetNode<Control>("modalError");
        modalHeader = modalError.GetNode<Label>("back/Header");
        modalDesc = modalError.GetNode<Label>("back/Text");
        modalOk = modalError.GetNode<Button>("back/OK");
        loadMenu = GetNode<LoadMenu>("Load");
        testingLevelsMenu = GetNode<TestingLevelsMenu>("Testing");

        settingsMenu = GetNode<SettingsMenu>("../SettingsMenu");

        var raceStr = Global.RaceToString(global.playerRace);
        var bgPicRes = GD.Load<CompressedTexture2D>("res://assets/textures/interface/bg_pic/bg_" + raceStr + ".png");

        var bgPic = GetNode<TextureRect>("bgPony");
        bgPic.Texture = bgPicRes;
    }

    private string GetMenuText(string phrase, string section = "main") 
    {
        return InterfaceLang.GetPhrase(menuName, section, phrase);
    }

    private async void ChangeLabel(Label label) 
    {
        label.VisibleRatio = 0;
        while(label.VisibleRatio < 1) 
        {
            label.VisibleRatio += 0.1f;
            await global.ToTimer(0.05f, this);
        }
        EmitSignal(SignalName.LabelChanged);
    }

    public override void LoadInterfaceLanguage()
    {
        continueButton.Text = GetMenuText("continue");
        startButton.Text = GetMenuText("start");
        loadButton.Text = GetMenuText("load");
        settingsButton.Text = GetMenuText("settings");
        aboutButton.Text = GetMenuText("about");
        exitButton.Text = GetMenuText("exit");
        testingButton.Text = GetMenuText("testing");
        
        modalHeader.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "header");
        modalDesc.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "desc");
        modalOk.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "ok");
        loadMenu.LoadInterfaceLanguage();
        testingLevelsMenu.LoadInterfaceLanguage();
    }

    private void LoadAboutLanguage()
    {
        aboutPage.Text = GetMenuText("page", "about");
        aboutLabel.Text = GetMenuText("label", "about");
        aboutBack.Text = GetMenuText("back");
    }

    private void LoadChooseAutosaveLanguage()
    {
        autosavePage.Text = GetMenuText("page", "autosave");
        autosaveHeader.Text = GetMenuText("header", "autosave");
        autosaveInput.PlaceholderText = GetMenuText("placeholder", "autosave");
        autosaveBack.Text = GetMenuText("back");
        autosaveNext.Text = GetMenuText("next");
    }

    private void LoadRaceLanguage()
    {
        racePage.Text = GetMenuText("page", "race");
        raceBack.Text = GetMenuText("back");

        for(int i = 0; i < raceButtons.Length; i++) 
        {
            raceButtons[i].Text = GetMenuText("button" + i.ToString(), "race");
            raceLabels[i].Text = GetMenuText("label" + i.ToString(), "race");
        }
    }

    public override void SoundClick()
    {
        audi.PlayClick();
    }

    protected override void SoundHover()
    {
        audi.PlayHover();
    }

    public override async void SetMenuVisible(bool animating = false)
    {
        Visible = true;
        pageLabel.Visible = true;

        if (animating) 
        {
            downLabel.Visible = false;
            label5.Visible = false;
            pageLabel.Text = GetMenuText("loading");
            await global.ToTimer(1, this);

            pageLabel.Text = GetMenuText("welcome");
            ChangeLabel(pageLabel);
            await global.ToTimer(1.5f, this);

            pageLabel.Text = GetMenuText("page");
            ChangeLabel(pageLabel);

            while(backgroundRect.Color.A > 0) {
                backgroundRect.Color = new Color(
                    0, 0, 0,
                    backgroundRect.Color.A - 0.1f
                );
                await ToSignal(GetTree(), "process_frame");
            }
        }
        else
        {
            pageLabel.Text = GetMenuText("page");
            backgroundRect.Color = new Color(0, 0, 0, 0);
        }
    
        music.Play();
        downLabel.Visible = true;
        label5.Visible = true;

        LoadInterfaceLanguage();
        continueButton.Visible = Global.saveFilesArray.Count > 0;
        startButton.Visible = true;
        loadButton.Visible = true;
        settingsButton.Visible = true;
        aboutButton.Visible = true;
        exitButton.Visible = true;
        testingButton.Visible = OS.IsDebugBuild();
    }

    private static Race GetRaceFromSave(string fileName)
    {
        var filePath = $"user://saves/{SaveMenu.GetLikeLatinString(fileName)}.sav";
        var saveFile = FileAccess.OpenCompressed(filePath, FileAccess.ModeFlags.Read);
        for (int i = 0; i < 4; i++)
        {
            saveFile.GetLine();
        }
        Race race = Global.RaceFromString(saveFile.GetLine());
        saveFile.Close();
        return race;
    }

    public override void _Ready()
    {
        global.LoadSettings(this);
        base._Ready();
        audi = GetNode<MenuAudi>("audi");
        music = GetNode<AudioStreamPlayer>("music");

        if (global.mainMenuFirstTime && Global.saveFilesArray.Count > 0)
        {
            string lastSave = global::LoadMenu.GetLastSaveFile();
            global.playerRace = GetRaceFromSave(lastSave);
        }
        
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

    public void _on_continue_pressed()
    {
        SoundClick();
        string lastSave = global::LoadMenu.GetLastSaveFile();
        if (!global::LoadMenu.TryToLoadGame(lastSave, GetNode<LevelsLoader>("/root/Main")))
        {
            modalError.Visible = true;
        }
    }

    public void _on_error_OK_pressed()
    {
        modalError.Visible = false;
    }

    public void _on_start_pressed()
    {
        SoundClick();
        LoadChooseAutosaveLanguage();
        autosaveMenu.Visible = true;
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

    public void _on_about_pressed()
    {
        LoadAboutLanguage();
        SoundClick();
        aboutMenu.Visible = true;
    }
    
    public void _on_testing_pressed()
    {
        SoundClick();
        testingLevelsMenu.Visible = true;
    }

    public void _on_back_pressed() 
    {
        SoundClick();
        
        if (changeRaceMenu.Visible)
        {
            changeRaceMenu.Visible = false;
            autosaveMenu.Visible = true;
        }
        else
        {
            continueButton.Visible = Global.saveFilesArray.Count > 0;
            aboutMenu.Visible = false;
            autosaveMenu.Visible = false;
            loadMenu.Visible = false;
            testingLevelsMenu.Visible = false;
        }
        
        _on_mouse_exited();
    }

    public async void _on_exit_pressed()
    {
        SoundClick();
        await global.ToTimer(0.3f, this);
        GetTree().Quit();
    }

    public void _on_autosave_next_pressed()
    {
        SoundClick();

        if (string.IsNullOrEmpty(autosaveInput.Text)) return;
        
        LoadRaceLanguage();
        autosaveMenu.Visible = false;
        changeRaceMenu.Visible = true;
        Global.Get().autosaveName = autosaveInput.Text;
    }

    public void _on_choose_pressed(InputEvent @event, string raceName)
    {
        if (!Input.IsActionJustPressed("ui_click")) return;
        Race newRace = Global.RaceFromString(raceName);
        global.playerRace = newRace;
        GetNode<LevelsLoader>("/root/Main").LoadLevel(1);
    }

    public void _on_selectArea_mouse_entered(string raceName, string areaName)
    {
        base._on_mouse_entered("race", raceName);
        Control newHover = changeRaceMenu.GetNode<Control>(areaName + "/hover");
        newHover.Visible = true;
        tempHover = newHover;
    }

    public void _on_selectArea_mouse_exited()
    {
        if (tempHover == null) return;
        base._on_mouse_exited();
        tempHover.Visible = false;
        tempHover = null;
    }
}
