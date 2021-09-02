using Godot;
using Godot.Collections;

public class PickItemTrigger : ActivateOtherTrigger
{
    [Export] public NodePath itemModelPath;
    [Export] public string itemCode;
    [Export] public string hintCode;

    private Spatial itemModel;
    private static Player player => Global.Get().player;

    public override void _Ready()
    {
        base._Ready();
        itemModel = GetNode<Spatial>(itemModelPath);
        SetProcess(false);
    }

    public override void _Process(float delta)
    {
        player.Camera.ShowHint(hintCode, false);

        if (string.IsNullOrEmpty(itemCode)) return;
        if (!Input.IsActionJustPressed("use")) return;
        
        itemModel.Visible = false;
        InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.AddOrDropItem(itemCode);
        player?.Camera.HideHint();

        Messages messages = GetNode<Messages>("/root/Main/Scene/canvas/messages");
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        messages.ShowMessage("itemTaken", itemData["name"].ToString(), "items");

        base._on_activate_trigger();
    }
    
    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        SetProcess(true);
    }

    public void _on_body_exited(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        player?.Camera.HideHint();
        SetProcess(false);
    }
}