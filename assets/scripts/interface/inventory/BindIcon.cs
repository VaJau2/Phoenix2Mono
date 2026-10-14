using Godot;

public partial class BindIcon : ItemIcon
{
    private AnimationPlayer anim;
    private bool isSelected;
    
    private TextureRect shadow;

    private Player player => Global.Get().player;

    [Signal]
    public delegate void IsDeletingEventHandler();
    
    public override async void _Ready()
    {
        base._Ready();
        shadow = GetNode<TextureRect>("icon/shadow");
        anim = GetNode<AnimationPlayer>("anim");
        anim.Play("RESET");
        
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        player.Connect(Player.SignalName.UseItem, new Callable(this, nameof(OnPlayerUseItem)));

        if (myItemCode == player.Inventory.weapon)
        {
            SetSelect(true);
        }
    }

    public override void SetIcon(CompressedTexture2D newIcon)
    {
        base.SetIcon(newIcon);
        shadow.Texture = newIcon;
    }

    public bool IsNeedDelay()
    {
        var itemData = ItemJSON.GetItemData(myItemCode);
        var type = (ItemType)(int)itemData["type"];

        return type == ItemType.food || type == ItemType.meds;
    }

    public async void DeleteWithDelay()
    {
        if (IsNeedDelay() && anim.IsPlaying())
        {
            await ToSignal(anim, "animation_finished");
        }
        
        EmitSignal(SignalName.IsDeleting);
        QueueFree();
    }

    private void OnPlayerUseItem(string itemCode)
    {
        var itemData = ItemJSON.GetItemData(itemCode);

        switch ((ItemType)(int)itemData["type"])
        {
            case ItemType.weapon:
                CheckWeaponIcon(itemCode);
                break;
            
            case ItemType.meds:
            case ItemType.food:
                if (myItemCode == itemCode) anim.Play("SelectOnce");
                break;
        }
    }

    private void CheckWeaponIcon(string itemCode)
    {
        if (isSelected)
        {
            SetSelect(false);
        }
        else if (myItemCode == itemCode && !player.IsSitting)
        {
            SetSelect(true);
        }
    }

    private void SetSelect(bool value)
    {
        anim.Play(value ? "Select" : "Deselect");
        isSelected = value;
    }
    
    public override void _on_itemIcon_mouse_entered() {}
    public override void _on_itemIcon_mouse_exited() {}
}
