using Godot;

public partial class ButtonIcon : IconWithShadow
{
    [Export] private string key;
    
    public override void _Ready()
    {
        base._Ready();
        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(LevelsLoader.SignalName.SaveDataLoaded, new Callable(this, nameof(OnSaveDataLoaded)));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        
        var settings = GetNode<ControlsSubmenu>("/root/Main/Menu/SettingsMenu/Controls");
        settings.Connect(ControlsSubmenu.SignalName.ChangeControlEvent, new Callable(this, nameof(UpdateIcon)));
        
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        var path = $"res://assets/textures/interface/icons/buttons/{Global.GetKeyName(key)}.png";
        var icon = GD.Load<Texture2D>(path);
        SetIcon(icon);
    }
}