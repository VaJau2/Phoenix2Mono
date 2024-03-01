using Godot;

public partial class PutItemTrigger : ActivateOtherTrigger
{
    [Export] private NodePath itemModelPath;
    [Export] private string itemCode;

    private Node3D itemModel;

    public override void _Ready()
    {
        itemModel = GetNode<Node3D>(itemModelPath);
        base._Ready();
    }

    public override void OnActivateTrigger()
    {
        itemModel.Visible = true;
        InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.RemoveItemIfExists(itemCode);
        
        base.OnActivateTrigger();
    }
}
