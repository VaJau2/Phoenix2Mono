using Godot;
using Godot.Collections;

public class PickItemTrigger : ActivateOtherTrigger
{
    [Export] private string itemCode;
    [Export] private NodePath itemModelPath;
    private Spatial itemModel;

    public override void _Ready()
    {
        itemModel = GetNode<Spatial>(itemModelPath);
        base._Ready();
    }

    public override void _on_activate_trigger()
    {
        InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.AddOrDropItem(itemCode);
        
        Global.AddDeletedObject(itemModel.Name);
        itemModel.QueueFree();
        
        Messages messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        messages.ShowMessage("itemTaken", itemData["name"].ToString(), "items");

        base._on_activate_trigger();
    }
}