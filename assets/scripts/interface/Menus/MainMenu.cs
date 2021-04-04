using System;
using Godot;

public class MainMenu : MenuBase
{
    Global global = Global.Get();
    AudioStreamPlayer audi;
    AudioStreamPlayer music;

    ColorRect backgroundRect;

    Control changeRaceMenu;
    LoadMenu loadMenu;
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

    Label aboutPage;
    Label aboutLabel;
    Button aboutBack;

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
    public delegate void labelChanged();

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

        aboutMenu = GetNode<Control>("About");
        aboutPage = GetNode<Label>("About/page_label");
        aboutLabel = GetNode<Label>("About/about_label");
        aboutBack = GetNode<Button>("About/back");

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

        settingsMenu = GetNode<SettingsMenu>("../SettingsMenu");

        var raceStr = Global.RaceToString(global.playerRace);
        var bgPicRes = GD.Load<StreamTexture>("res://assets/textures/interface/bg_pic/bg_" + raceStr + ".png");

        var bgPic = GetNode<TextureRect>("bgPony");
        bgPic.Texture = bgPicRes;
    }

    private string getMenuText(string phrase, string section = "main") {
        return InterfaceLang.GetPhrase(menuName, section, phrase);
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

    public override void loadInterfaceLanguage()
    {
        continueButton.Text = getMenuText("continue");
        startButton.Text = getMenuText("start");
        loadButton.Text = getMenuText("load");
        settingsButton.Text = getMenuText("settings");
        aboutButton.Text = getMenuText("about");
        exitButton.Text = getMenuText("exit");
        
        modalHeader.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "header");
        modalDesc.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "desc");
        modalOk.Text = InterfaceLang.GetPhrase("saveloadMenu", "modal", "ok");
        loadMenu.LoadInterfaceLanguage();
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

    public override async void SetMenuVisible(bool animating = false)
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

            while(backgroundRect.Color.a > 0) {
                backgroundRect.Color = new Color(
                    0, 0, 0,
                    backgroundRect.Color.a - 0.1f
                );
                await ToSignal(GetTree(), "idle_frame");
            }
        }
        else
        {
            pageLabel.Text = getMenuText("page");
            backgroundRect.Color = new Color(0, 0, 0, 0);
        }
    
        //music.Play();
        downLabel.Visible = true;
        label5.Visible = true;

        loadInterfaceLanguage();
        continueButton.Visible = Global.saveFilesArray.Count > 0;
        startButton.Visible = true;
        loadButton.Visible = true;
        settingsButton.Visible = true;
        aboutButton.Visible = true;
        exitButton.Visible = true;
    }

    private static string GetLastSaveFile()
    {
        if (Global.saveFilesArray.Count == 0) return null;
        
        string lastFileName = null;
        ulong lastTime = 0;

        foreach (FileTableLine tempTableLine in Global.saveFilesArray)
        {
            var fileName = tempTableLine.name;
            var filePath = $"res://saves/{SaveMenu.GetLikeLatinString(fileName)}.sav";
            var tempTime = new File().GetModifiedTime(filePath);
            if (tempTime <= lastTime) continue;
            lastFileName = fileName;
            lastTime = tempTime;
        }

        return lastFileName;
    }

    private static Race GetRaceFromSave(string fileName)
    {
        var saveFile = new File();
        var filePath = $"res://saves/{SaveMenu.GetLikeLatinString(fileName)}.sav";
        saveFile.OpenCompressed(filePath, File.ModeFlags.Read);
        for (int i = 0; i < 3; i++) saveFile.GetLine();
        return  Global.RaceFromString(saveFile.GetLine());
    }

    public override void _Ready()
    {
        global.LoadSettings(this);
        base._Ready();
        audi = GetNode<AudioStreamPlayer>("audi");
        music = GetNode<AudioStreamPlayer>("music");

        if (global.mainMenuFirstTime && Global.saveFilesArray.Count > 0)
        {
            string lastSave = GetLastSaveFile();
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
        string lastSave = GetLastSaveFile();
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
        loadRaceLanguage();
        changeRaceMenu.Visible = true;
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
