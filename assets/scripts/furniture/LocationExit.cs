using Godot;

public class LocationExit : TriggerBase
{
    [Export] private bool UseBlackScreen = false;
    [Export] private bool SaveInventory = true;
    [Export] private bool SaveOldInventorySave = false;

    [Export] private int newLevelNum;

    private LevelsLoader levelsLoader;
    private ColorRect blackScreen;

    public override void _Ready()
    {
        levelsLoader = GetNode<LevelsLoader>("/root/Main");
        blackScreen = GetNode<ColorRect>("/root/Main/Scene/canvas/black");
        
        SetProcess(false);
    }
    
    public void _on_player_exited(Node body)
    {
        if (Global.Get().player != null)
        {
            _on_activate_trigger();
        }
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        var saveNode = GetNode<SaveNode>("/root/Main/SaveNode");
        
        if (SaveOldInventorySave)
        {
            saveNode.CloneSaveData(levelsLoader);
        }
        
        if (SaveInventory)
        {
            saveNode.InventoryData = Global.Get().player.inventory.GetSaveData(false);
        }

        if (UseBlackScreen)
        {
            SetProcess(true);
        }
        else
        {
            levelsLoader
                .SetUseLoadingMenu(true)
                .LoadLevel(newLevelNum);
        }
    }
    
    public override void _Process(float delta)
    {
        blackScreen.Visible = true;
        var blackScreenColor = blackScreen.Color;
        if (blackScreenColor.a < 1)
        {
            blackScreenColor.a += delta;
            blackScreen.Color = blackScreenColor;
        }
        else
        {
            SetProcess(false);
            
            levelsLoader
                .SetUseLoadingMenu(false)
                .LoadLevel(newLevelNum);
        }
    }
}