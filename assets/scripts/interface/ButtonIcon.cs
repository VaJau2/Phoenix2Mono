using Godot;

public class ButtonIcon : IconWithShadow
{
    [Export] private string key;
    
    public override void _Ready()
    {
        base._Ready();
        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");
        
        var settings = GetNode<ControlsSubmenu>("/root/Main/Menu/SettingsMenu/Controls");
        settings.Connect(nameof(ControlsSubmenu.ChangeControlEvent), this, nameof(UpdateIcon));
        
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        var path = $"res://assets/textures/interface/icons/buttons/{Global.GetKeyName(key)}.png";
        var icon = GD.Load<Texture>(path);
        SetIcon(icon);
    }
}