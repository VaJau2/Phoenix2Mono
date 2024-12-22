using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class TestingLevelsMenu : Control
{
    private const string AUTOSAVE_NAME = "testing";
    private const int AMMO_COUNT = 50;
    
    [Signal]
    public delegate void BackPressed();

    private LevelsLoader levelsLoader;
    private MenuBase parentMenu;
    
    private Label pageLabel;
    private Label levelsHeader;
    private Label itemsHeader;
    private Label moneyHeader;
    private Label raceHeader;
    private Label questsHeader;
    
    private Button backButton;
    private Button loadButton;

    private LineEdit itemsList;
    private SpinBox moneyInput;
    private OptionButton raceList;
    private OptionButton levelsList;
    private TextEdit questsInput;

    public void LoadInterfaceLanguage()
    {
        pageLabel.Text = InterfaceLang.GetPhrase("testingMenu", "main", "page_header");
        backButton.Text = InterfaceLang.GetPhrase("testingMenu", "main", "back");
        loadButton.Text = InterfaceLang.GetPhrase("testingMenu", "main", "load");
        levelsHeader.Text = InterfaceLang.GetPhrase("testingMenu", "main", "levels_header");
        itemsHeader.Text = InterfaceLang.GetPhrase("testingMenu", "main", "items_header");
        moneyHeader.Text = InterfaceLang.GetPhrase("testingMenu", "main", "money_header");
        questsHeader.Text = InterfaceLang.GetPhrase("testingMenu", "main", "quests_header");
        raceHeader.Text = InterfaceLang.GetPhrase("testingMenu", "main", "race_header");
    }

    public override void _Ready()
    {
        levelsLoader = GetNode<LevelsLoader>("/root/Main");
        parentMenu = GetParent<MenuBase>();
        
        pageLabel = GetNode<Label>("pageLabel");
        backButton = GetNode<Button>("back");
        loadButton = GetNode<Button>("load");
        levelsHeader = GetNode<Label>("levelsHeader");
        itemsHeader = GetNode<Label>("itemsHeader");
        moneyHeader = GetNode<Label>("moneyHeader");
        raceHeader = GetNode<Label>("raceHeader");
        questsHeader = GetNode<Label>("questsHeader");
        
        levelsList = GetNode<OptionButton>("levelsList");
        itemsList = GetNode<LineEdit>("itemsList");
        moneyInput = GetNode<SpinBox>("moneyInput");
        raceList = GetNode<OptionButton>("raceList");
        questsInput = GetNode<TextEdit>("questsInput");
        LoadLevelsList();
        LoadRacesList();
    }

    private void LoadLevelsList()
    {
        var levelsData = Global.LoadJsonFile("scenes/levels.json");
        foreach (var levelName in levelsData.Values)
        {
            levelsList.AddItem(levelName.ToString());
        }
        
        levelsList.Select(0);
    }

    private void LoadRacesList()
    {
        foreach (var race in Enum.GetValues(typeof(Race)))
        {
            raceList.AddItem(race.ToString());
        }
    }
    
    public void _on_back_pressed()
    {
        parentMenu.SoundClick();
        EmitSignal(nameof(BackPressed));
    }
    
    public void _on_mouse_entered(string section, string messageLink)
    {
        parentMenu._on_mouse_entered(section, messageLink, "testingMenu");
    }

    public void _on_mouse_exited()
    {
        parentMenu._on_mouse_exited();
    }

    public void _on_load_pressed()
    {
        var chosenLevel = levelsList.Selected + 1;
        if (chosenLevel <= 0) return;

        var global = Global.Get();
        global.autosaveName = AUTOSAVE_NAME;
        global.playerRace = Global.RaceFromString(
            raceList.GetItemText(raceList.Selected)
                .ToLower()
            );
        
        InitSavableVariables();

        if (string.IsNullOrEmpty(itemsList.Text))
        {
            levelsLoader.LoadLevel(chosenLevel);
        }
        else
        {
            levelsLoader.LoadLevel(chosenLevel, MakeLoadData(), new Array());
        }
    }

    private Dictionary MakeLoadData()
    {
        var inventoryData = new Dictionary
        {
            {"itemCodes", ParseItemCodes()},
            {"itemCounts", MakeItemCounts()},
            {"tempKeys", MakeKeys()},
            {"itemBinds", new Array()},
            {"money", moneyInput.Value},
            {"weapon", ""},
            {"cloth", ""},
            {"artifact", ""},
            {"weaponBind", ""},
            {"effectNames", new Array()},
            {"effectTimes", new Array()},
        };
        
        return new Dictionary
        {
            {
                "Player",
                new Dictionary
                {
                    { "inventory", inventoryData }
                }
            }
        };
    }

    private Array ParseItemCodes()
    {
        var result = new Array();

        foreach (var item in itemsList.Text.Split(','))
        {
            result.Add(item.Trim());
        }

        return result;
    }
    
    private Array MakeItemCounts()
    {
        var result = new Array();

        foreach (var item in itemsList.Text.Split(','))
        {
            result.Add(item.Contains("ammo") ? AMMO_COUNT : 0);
        }

        return result;
    }

    private Array MakeKeys()
    {
        var result = new Array();

        foreach (var item in itemsList.Text.Split(','))
        {
            if (item.Contains("key")) 
            {
                result.Add(item.Trim());
            }
        }
        
        return result;
    }

    private void InitSavableVariables()
    {
        var saveNode = GetNode<SaveNode>("/root/Main/SaveNode");
        if (saveNode == null) return;

        var savableVarsText = questsInput.Text;
        if (string.IsNullOrEmpty(savableVarsText)) return;
        
        var resultJson = JSON.Parse(savableVarsText);

        if (resultJson.Error != Error.Ok) return;
        var savableVars = (Dictionary)resultJson.Result;

        foreach (var key in savableVars.Keys)
        {
            saveNode.SavedVariables[key] = savableVars[key];
        }
    }
}
