using Godot;
using Godot.Collections;

public class LevelsLoader : Node
{
	[Export] private Environment defaultEnvironment;
	private WorldEnvironment tempDefaultEnvironment;
	
	Global global = Global.Get();
	public static int tempLevelNum = 0;
	
	private Array<string> levelPaths = new Array<string>();
	private Control currentMenu;
	private Node currentScene;
	private Control currentLoading;
	private Node menuParent;
	
	private bool useLoadingMenu = true;
	private PackedScene mainMenuPrefab;
	private PackedScene pauseMenuPrefab;
	private PackedScene loadingMenuPrefab;
	private PackedScene deathMenuPrefab;
	private bool mainMenuOn = true;
	private ResourceInteractiveLoader loader;

	[Signal]
	public delegate void LevelLoaded();

	[Signal]
	public delegate void SaveDataLoaded();

	private bool loadSavedData;
	private Dictionary levelData;
	private Array deletedObjects;

	public LevelsLoader SetUseLoadingMenu(bool value)
	{
		useLoadingMenu = value;
		return this;
	}
	
	public override async void _Ready()
	{
		var levelsData = Global.LoadJsonFile("scenes/levels.json");
		levelPaths.Add("menu");
		foreach(string filePath in levelsData.Values) {
			levelPaths.Add("res://scenes/" + filePath);
		}
		
		mainMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/MainMenu.tscn");
		pauseMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/PauseMenu.tscn");
		loadingMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/LoadingMenu.tscn");
		deathMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/DeathMenu.tscn");

		await ToSignal(GetTree(), "idle_frame");
		
		menuParent = GetNode<Node>("Menu");
		currentMenu = menuParent.GetNode<Control>(
			menuParent.HasNode("MainMenu") ? "MainMenu" : "PauseMenu"
		);

		if (!HasNode("/root/Main/Scene")) return;
		
		//если загружена тестовая сцена
		currentScene = GetNode("/root/Main/Scene");
		mainMenuOn = false;
	}

	private void HandleCustomEvents()
	{
		if (IsInstanceValid(Global.Get().player))
		{
			Global.Get().player.Inventory.effects.OnLoadOtherLevel();
		}
	}

	private void RespawnMenu(PackedScene newMenu) 
	{
		currentMenu.Free();
		currentMenu = (Control)newMenu.Instance();
		menuParent.AddChild(currentMenu);
		menuParent.MoveChild(currentMenu, 0);
	}

	private async void UpdateScene()
	{
		global.SetPause(this, false);
		Engine.TimeScale = 1f;
		
		if (levelPaths[tempLevelNum] == "menu") 
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else
		{
			if (useLoadingMenu)
			{
				currentLoading = (Control)loadingMenuPrefab.Instance();
				menuParent.AddChild(currentLoading);
			}
			else
			{
				useLoadingMenu = true;
			}
			
			if (loader != null) return;
			loader = ResourceLoader.LoadInteractive(levelPaths[tempLevelNum]);
			SetProcess(true);
		}

		if (currentScene == null) return;
		
		global.player = null;
		currentScene.QueueFree();
		currentScene = null;
		await ToSignal(GetTree(), "idle_frame");
		SpawnDefaultEnvironment();
	}

	private void SpawnDefaultEnvironment()
	{
		var defaultEnv = new WorldEnvironment();
		defaultEnv.Environment = defaultEnvironment;
		AddChild(defaultEnv);
		tempDefaultEnvironment = defaultEnv;
	}

	private void UpdateMenu()
	{
		if (tempLevelNum != 0) 
		{
			//грузим меню паузы
			if (currentMenu.Name != "PauseMenu") 
			{
				RespawnMenu(pauseMenuPrefab);
				mainMenuOn = false;
			}
			else
			{
				MenuManager.CloseMenu(currentMenu as IMenu);
			}

			currentMenu.Visible = false;
		} else 
		{
			//грузим главное меню
			if (!mainMenuOn) 
			{
				RespawnMenu(mainMenuPrefab);
				mainMenuOn = true;
			}

			currentMenu.Visible = true; 
		}
	}

	public void ShowDeathMenu()
	{
		global.SetPause(this, true);
		RespawnMenu(deathMenuPrefab);
		currentMenu.Visible = true;
	}

	//загрузка уровня
	public void LoadLevel(int levelNum)
	{
		HandleCustomEvents();
		
		if (loadSavedData && levelNum == 0)
		{
			loadSavedData = false;
		}
		
		tempLevelNum = levelNum;
		Global.deletedObjects.Clear();
		
		if (!loadSavedData)
		{
			levelData = null;
			deletedObjects = null;
		}
		
		CallDeferred(nameof(UpdateScene));
		CallDeferred(nameof(UpdateMenu));
	}

	//загрузка уровня с сохраненными данными
	public void LoadLevel(int levelNum, Dictionary levelData, Array deletedObjects)
	{
		loadSavedData = true;
		this.levelData = levelData;
		this.deletedObjects = deletedObjects;
		LoadLevel(levelNum);
	}

	private void LoadLevelData(Node scene)
	{
		//очищаем спавнер от предзаполненных вещей и создаем объект игрока в спавнере
		var playerSpawner = scene.GetNode<PlayerSpawner>("PlayerSpawner");
		playerSpawner.loadStartItems = false;

		//удаление удаленных в сохранении объектов
		foreach (string objName in deletedObjects)
		{
			Global.AddDeletedObject(objName);
			var foundedObject = Global.FindNodeInScene(scene, objName);
			foundedObject?.Free();
		}

		foreach (string objKey in levelData.Keys)
		{
			//создание создаваемых в сохранении объектов
			if (!objKey.BeginsWith("Created_")) continue;
			
			Dictionary objData = (Dictionary) levelData[objKey];
			var filename = objData["fileName"].ToString();
			var parentName = objData["parent"].ToString();

			PackedScene newNode = (PackedScene)GD.Load(filename);
			Node newInstance = newNode.Instance();
			newInstance.Name = objKey;
			var parent = Global.FindNodeInScene(scene, parentName);
			parent?.AddChild(newInstance);
		}
	}

	private async void LoadLevelObjects(Node scene)
	{
		//ждем загрузки сцены
		await ToSignal(GetTree(), "idle_frame");

		foreach (string objKey in levelData.Keys)
		{
			//загрузка загружаемых объектов

			//если это created-нод, у него хранится только имя
			//если это существующий нод, у него хранится путь от /root/Main...
			Node node = !objKey.BeginsWith("Created_") 
				? scene.GetNodeOrNull(objKey) 
				: Global.FindNodeInScene(scene, objKey);
			if (node is ISavable savable)
			{
				Dictionary objData = (Dictionary) levelData[objKey];
				savable.LoadData(objData);
			}
		}
		
		levelData = null;
		deletedObjects = null;
		loadSavedData = false;
	}

	private async void DeleteLoadingMenu() 
	{
		if (currentLoading == null) return;
		
		await ToSignal(GetTree(), "idle_frame");
		
		currentLoading.QueueFree();
		currentLoading = null;
	}
	
	public override void _Process(float delta)
	{
		if (loader == null) 
		{
			SetProcess(false);
			return;
		}

		var err = loader.Poll();
		if (err == Error.FileEof) 
		{
			tempDefaultEnvironment?.Free();
			
			var resource = (PackedScene)loader.GetResource();
			loader.Dispose();
			loader = null;
			
			var newScene = resource.Instance();
			
			if (loadSavedData)
			{
				LoadLevelData(newScene);
			}
			
			GetTree().Root.GetNode("Main").AddChild(newScene);
			currentScene = newScene;
			DeleteLoadingMenu();
			EmitSignal(nameof(LevelLoaded));

			if (loadSavedData)
			{
				LoadLevelObjects(newScene);
			}
			
			EmitSignal(nameof(SaveDataLoaded));
		}
		else if (err != Error.Ok) 
		{
			GD.PrintErr(err);
		}
	}
}
