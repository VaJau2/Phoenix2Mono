using Godot;

public class PutItemTrigger : ActivateOtherTrigger
{
    [Export] private NodePath itemModelPath;
    [Export] private string itemCode;

    private Spatial itemModel;

    public override void _Ready()
    {
        itemModel = GetNode<Spatial>(itemModelPath);
        base._Ready();
    }

    public override void _on_activate_trigger()
    {
        itemModel.Visible = true;
        InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.RemoveItemIfExists(itemCode);
        
        base._on_activate_trigger();
    }
}
