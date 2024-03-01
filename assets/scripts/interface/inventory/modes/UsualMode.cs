using Godot;

public partial class UsualMode: InventoryMode 
{
    private bool countClickTimer;
    private float clickTimer;

    public UsualMode (InventoryMenu menu) : base(menu) { }

    public void SetUseBindsCooldown(float cooldown)
    {
        bindsHandler.useCooldown = cooldown;
    }

    public override void CloseMenu()
    {
        CloseModal();
        base.CloseMenu();
    }

    public override void Process(float delta)
    {
        bindsHandler.UpdateUseCooldown(delta);
    }

    public override void UpdateInput(InputEvent @event)
    {
        base.UpdateInput(@event);
        
        if (menu.isOpen && tempButton != null) 
        {
            if (UpdateDragging(@event)) return;

            if (Input.IsMouseButtonPressed(MouseButton.Right) && !isDragging && tempButton != null) 
            {
                useHandler.DropTempItem();
            }

            if (@event is InputEventKey) 
            {
                bindsHandler.BindHotkeys(tempItemData["type"].As<ItemType>());
            }
        }
    }
}
