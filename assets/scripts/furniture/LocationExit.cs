using System;
using Godot;
using Godot.Collections;

public class LocationExit : StaticBody, IActivated, IInteractable, ISavable
{
    [Export] private string hintCode = "exitLocation";
    
    [Export] private bool SaveInventory = true;
    [Export] private bool SaveOldInventorySave = false;

    [Export] private int newLevelNum;
    
    public bool IsActive { get; private set; }
    
    public bool MayInteract => IsActive;

    public string InteractionHintCode => hintCode;

    public void SetActive(bool newActive)
    {
        IsActive = newActive;
    }
    
    public void Interact(PlayerCamera interactor)
    {
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
    
    public virtual Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"active", IsActive}
        };
    }
    
    public virtual void LoadData(Dictionary data)
    {
        IsActive = Convert.ToBoolean(data["active"]);
    }
}