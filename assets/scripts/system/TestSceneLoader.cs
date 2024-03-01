using Godot;

//Позволяет запускать тестовые сцены напрямую без необходимости копировать туда ноды менюшек
public partial class TestSceneLoader : Node
{
    [Export] private Race playerRace;
    
    public override async void _Ready()
    {
        //загружаем настройки и расу
        var global = Global.Get();
        global.LoadSettings(this);
        global.playerRace = playerRace;
        global.mainMenuFirstTime = false;
        
        //загружаем и скрываем менюшки паузы и настроек (если их нет на сцене)
        if (HasNode("/root/Main/Menu")) return;

        await ToSignal(GetTree(), "idle_frame");
        
        var main = GetNode("/root/Main");
        var canvas = new CanvasLayer();
        canvas.Name = "Menu";
        canvas.Layer = 2;
        main.AddChild(canvas);
        
        var pauseMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/PauseMenu.tscn");
        var settingsMenuPrefab = GD.Load<PackedScene>("res://objects/interface/menus/SettingsMenu.tscn");
        var pauseMenu = pauseMenuPrefab.Instantiate<Control>();
        var settingsMenu = settingsMenuPrefab.Instantiate<Control>();
        
        canvas.AddChild(settingsMenu);
        canvas.AddChild(pauseMenu);
        canvas.MoveChild(pauseMenu, 0);

        settingsMenu.Visible = false;
        pauseMenu.Visible = false;
    }
}
