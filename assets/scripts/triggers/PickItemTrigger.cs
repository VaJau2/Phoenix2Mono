using Godot;
using Godot.Collections;

public partial class PickItemTrigger : ActivateOtherTrigger
{
    [Export] private string itemCode;
    [Export] private NodePath itemModelPath;
    private Node3D itemModel;

    public override void _Ready()
    {
        itemModel = GetNode<Node3D>(itemModelPath);
        base._Ready();
    }

    public override void OnActivateTrigger()
    {
        InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.AddOrDropItem(itemCode);
        
        Global.AddDeletedObject(itemModel.Name);
        itemModel.QueueFree();
        
        Messages messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        messages.ShowMessage("itemTaken", itemData["name"].ToString(), "items");

        base.OnActivateTrigger();
    }
}