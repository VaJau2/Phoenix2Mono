using System;
using Godot;
using Godot.Collections;

public class PickItemTrigger : StaticBody, IActivated, IInteractable, ISavable
{
    [Export] public string itemCode;
    [Export] public string hintCode;
    [Export] public bool IsActive { get; private set; }
    
    public bool MayInteract => IsActive;
    public string InteractionHintCode => hintCode;
    
    public void SetActive(bool newActive)
    {
        IsActive = newActive;
    }

    public void Interact(PlayerCamera interactor)
    {
        if (string.IsNullOrEmpty(itemCode)) return;
        if (!Input.IsActionJustPressed("use")) return;
        
        InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.AddOrDropItem(itemCode);
        
        Messages messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        messages.ShowMessage("itemTaken", itemData["name"].ToString(), "items");
        
        var activateTrigger = GetNodeOrNull<ActivateTrigger>("activateTrigger");
        activateTrigger?._on_activate_trigger();
        
        Global.AddDeletedObject(Name);
        QueueFree();
        IsActive = false;
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
        Visible = IsActive;
    }
}