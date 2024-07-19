using Godot;

public class ButtonIcon : TextureRect
{
    [Export] private string key;
    
    public override void _Ready()
    {
        var parent = GetParent<Button>();
        parent.Connect("pressed", this, nameof(_on_mouse_exited));
        parent.Connect("mouse_entered", this, nameof(_on_mouse_entered));
        parent.Connect("mouse_exited", this, nameof(_on_mouse_exited));
        
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
        Texture = GD.Load<Texture>($"res://assets/textures/interface/icons/buttons/{Global.GetKeyName(key)}.png");
    }
    
    public void _on_mouse_entered()
    {
        Modulate = Colors.Black;
    }

    public void _on_mouse_exited()
    {
        Modulate = Colors.White;
    }
}
