using Godot;
using Godot.Collections;

public class LevelsLoader : Node
{
    Global global = Global.Get();
    private int tempLevelNum = 0;
    private Array<string> levelPaths = new Array<string>();
    private Control currentMenu;
    private Node currentScene;
    private Control currentLoading;
    private Node menuParent;
    
    private PackedScene mainMenuPrefab;
    private PackedScene pauseMenuPrefab;
    private PackedScene loadingMenuPrefab;
    private bool mainMenuOn = true;
    private ResourceInteractiveLoader loader;

    public override void _Ready()
    {
        var levelData = Global.loadJsonFile("scenes/levels.json");
        levelPaths.Add("menu");
        foreach(string filePath in levelData.Values) {
            levelPaths.Add("res://scenes/" + filePath);
        }
        currentMenu = GetNode<Control>("Menu/MainMenu");
        menuParent = GetNode<Node>("Menu");
        mainMenuPrefab = GD.Load<PackedScene>("res://objects/interface/MainMenu.tscn");
        pauseMenuPrefab = GD.Load<PackedScene>("res://objects/interface/PauseMenu.tscn");
        loadingMenuPrefab = GD.Load<PackedScene>("res://objects/interface/LoadingMenu.tscn");
    }

    private async void updateScene()
    {
        if(currentScene != null) {
            global.player = null;
            currentScene.QueueFree();
            currentScene = null;
        }

        global.SetPause(this, false);
        if (levelPaths[tempLevelNum] == "menu") {
            Input.SetMouseMode(Input.MouseMode.Visible);
            return;
        }
        
        currentLoading = (Control)loadingMenuPrefab.Instance();
        menuParent.AddChild(currentLoading);

        await ToSignal(GetTree(),"idle_frame");

        loader = ResourceLoader.LoadInteractive(levelPaths[tempLevelNum]);
        SetProcess(true);

        // var resource = (PackedScene)GD.Load(levelPaths[tempLevelNum]);
        // var newScene = resource.Instance();
        // GetTree().Root.GetNode("Main").AddChild(newScene);
        // currentScene = newScene;
    }

    private void respawnMenu(PackedScene newMenu) 
    {
        currentMenu.QueueFree();
        currentMenu = (Control)newMenu.Instance();
        menuParent.AddChild(currentMenu);
    }

    private void updateMenu()
    {
        if (tempLevelNum != 0) {
            //грузим меню паузы
            if (mainMenuOn) {
                respawnMenu(pauseMenuPrefab);
                mainMenuOn = false;
            }

            currentMenu.Visible = false;
        } else {
            //грузим главное меню
            if (!mainMenuOn) {
                respawnMenu(mainMenuPrefab);
                mainMenuOn = true;
            }

            currentMenu.Visible = true; 
        }
    }

    public void LoadLevel(int levelNum) 
    {
        tempLevelNum = levelNum;
        updateScene();
        updateMenu();
    }

    public void LoadNextLevel()
    {
        tempLevelNum += 1;
        updateScene();
        updateMenu();
    }

    private async void deleteLoadingMenu() 
    {
        await ToSignal(GetTree(), "idle_frame");
        currentLoading.QueueFree();
        currentLoading = null;
    }


    public override void _Process(float delta)
    {
        if (loader == null) {
            SetProcess(false);
            return;
        }

        var err = loader.Poll();
        if (err == Error.FileEof) {
            var resource = (PackedScene)loader.GetResource();
            loader.Dispose();
            loader = null;
            
            var newScene = resource.Instance();
            GetTree().Root.GetNode("Main").AddChild(newScene);
            currentScene = newScene;

            deleteLoadingMenu();
        }
        else if(err != Error.Ok) {
            GD.PrintErr(err);
        }
    }
}