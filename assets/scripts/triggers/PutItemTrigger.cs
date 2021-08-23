using Godot;

public class PutItemTrigger : ActivateOtherTrigger
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
        
        itemModel.Visible = true;
        InventoryMenu inventory = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.RemoveItemIfExists(itemCode);
        player?.Camera.HideHint();

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
