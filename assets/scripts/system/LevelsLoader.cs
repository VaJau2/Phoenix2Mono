using System;
using Godot;
using Godot.Collections;

public class LevelsLoader : Node
{
	Global global = Global.Get();
	public static int tempLevelNum = 0;
	
	private Array<string> levelPaths = new Array<string>();
	private Control currentMenu;
	private Node currentScene;
	private Control currentLoading;
	private Node menuParent;
	
	private PackedScene mainMenuPrefab;
	private PackedScene pauseMenuPrefab;
	private PackedScene loadingMenuPrefab;
	private PackedScene dealthMenuPrefab;
	private bool mainMenuOn = true;
	private ResourceInteractiveLoader loader;
	
	[Signal]
	public delegate void LevelLoaded();

	public override void _Ready()
	{
		var levelData = Global.loadJsonFile("scenes/levels.json");
		levelPaths.Add("menu");
		foreach(string filePath in levelData.Values) {
			levelPaths.Add("res://scenes/" + filePath);
		}
		currentMenu = GetNode<Control>("Menu/MainMenu");
		menuParent = GetNode<Node>("Menu");
		mainMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/MainMenu.tscn");
		pauseMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/PauseMenu.tscn");
		loadingMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/LoadingMenu.tscn");
		dealthMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/DealthMenu.tscn");
	}

	private void respawnMenu(PackedScene newMenu) 
	{
		currentMenu.QueueFree();
		currentMenu = (Control)newMenu.Instance();
		menuParent.AddChild(currentMenu);
		menuParent.MoveChild(currentMenu, 0);
	}

	private async void updateScene()
	{
		if (currentScene != null) {
			global.player = null;
			currentScene.QueueFree();
			currentScene = null;
			await ToSignal(GetTree(), "idle_frame");
		}

		global.SetPause(this, false);
		Engine.TimeScale = 1f;
		if (levelPaths[tempLevelNum] == "menu") {
			Input.SetMouseMode(Input.MouseMode.Visible);
			return;
		}

		if (loader == null) {
			currentLoading = (Control)loadingMenuPrefab.Instance();
			menuParent.AddChild(currentLoading);

			loader = ResourceLoader.LoadInteractive(levelPaths[tempLevelNum]);
			SetProcess(true);
		}
	}

	private void updateMenu()
	{
		if (tempLevelNum != 0) {
			//грузим меню паузы
			if (currentMenu.Name != "PauseMenu") {
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

	public void ShowDealthMenu()
	{
		global.SetPause(this, true);
		respawnMenu(dealthMenuPrefab);
		currentMenu.Visible = true;
	}

	public void LoadLevel(int levelNum) 
	{
		tempLevelNum = levelNum;
		updateScene();
		updateMenu();
	}

	public async void LoadLevel(int levelNum, Dictionary levelData, Godot.Collections.Array deletedObjects)
	{
		LoadLevel(levelNum);
		await ToSignal(this, nameof(LevelLoaded));
		await ToSignal(GetTree(), "idle_frame");

		//загрузка сохранения
		Node scene = GetNode("/root/Main/Scene");
		
		//удаление удаленных в сохранении объектов
		foreach (string objName in deletedObjects)
		{
			var foundedObject = Global.FindNodeInScene(scene, objName);
			foundedObject.QueueFree();
		}

		foreach (string objKey in levelData.Keys)
		{
			Dictionary objData = (Dictionary) levelData[objKey];
			
			//создание создаваемых в сохранении объектов
			if (objKey.BeginsWith("Created_"))
			{
				string className = objData["class_name"].ToString();
				string parentName = objData["parent"].ToString();
				
				var scriptType = Type.GetType(className);
				if (scriptType == null) throw new InvalidCastException();
				var scriptObj = Activator.CreateInstance(scriptType) as ISavable;
				var parent = Global.FindNodeInScene(scene, parentName);
				
				scriptObj?.LoadData(objData);
				parent?.AddChild((Node)scriptObj);
			}
			
			//загрузка загружаемых объектов
			else
			{
				ISavable node = Global.FindNodeInScene(scene, objKey) as ISavable;
				node?.LoadData(objData);
			}
		}
	}

	public void ReloadLevel()
	{
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
			EmitSignal(nameof(LevelLoaded));
		}
		else if(err != Error.Ok) {
			GD.PrintErr(err);
		}
	}
}
