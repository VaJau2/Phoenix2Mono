using Godot;
using System;

public class DeleteItemTrigger : TriggerBase
{
    [Export] public string ItemToCheck;
    
    
    public override async void _Ready()
    {
        if (!IsActive) return;

        await ToSignal(GetTree(), "idle_frame");
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (!string.IsNullOrEmpty(ItemToCheck))
        {
            InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            inventory.RemoveItemIfExists(ItemToCheck);
        }

        base._on_activate_trigger();
    }
}
