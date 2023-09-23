using System;
using Godot;
using Godot.Collections;

public class LocationExit : TriggerBase
{
    [Export] private bool SaveInventory = true;
    [Export] private bool SaveOldInventorySave = false;

    [Export] private int newLevelNum;

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        var saveNode = GetNode<SaveNode>("/root/Main/SaveNode");
        
        if (SaveOldInventorySave)
        {
            saveNode.CloneSaveData(levelsLoader);
        }
        
        if (SaveInventory)
        {
            saveNode.InventoryData = Global.Get().player.inventory.GetSaveData(false);
        }
        
        levelsLoader.LoadLevel(newLevelNum);
    }
}