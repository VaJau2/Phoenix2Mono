using System;
using Godot;
using Godot.Collections;

public class PutItemTrigger : StaticBody, IActivated, IInteractable, ISavable
{
    [Export] public NodePath itemModelPath;
    [Export] public string itemCode;
    [Export] public string hintCode;

    [Export] public bool IsActive { get; private set; }
    [Export] public bool DeleteAfterPut = true;

    public bool MayInteract => IsActive;
    public string InteractionHintCode => hintCode;

    private Spatial itemModel;

    public override void _Ready()
    {
        itemModel = GetNode<Spatial>(itemModelPath);
    }
    
    public void SetActive(bool newActive)
    {
        IsActive = newActive;
    }
    
    public void Interact(PlayerCamera interactor)
    {
        if (string.IsNullOrEmpty(itemCode)) return;
        if (!Input.IsActionJustPressed("use")) return;
        
        itemModel.Visible = true;
        InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.RemoveItemIfExists(itemCode);
        
        var activateTrigger = GetNodeOrNull<ActivateTrigger>("activateTrigger");
        activateTrigger?._on_activate_trigger();
        
        if (!DeleteAfterPut) return;
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
