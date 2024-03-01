using Godot;
using System;

public partial class DeleteItemTrigger : TriggerBase
{
    [Export] public string ItemToCheck;
    
    
    public override async void _Ready()
    {
        if (!IsActive) return;

        await ToSignal(GetTree(), "idle_frame");
        OnActivateTrigger();
    }

    public override void OnActivateTrigger()
    {
        if (!string.IsNullOrEmpty(ItemToCheck))
        {
            InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            inventory.RemoveItemIfExists(ItemToCheck);
        }

        base.OnActivateTrigger();
    }
}
