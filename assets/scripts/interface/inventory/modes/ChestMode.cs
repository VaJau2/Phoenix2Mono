using Godot;
using Godot.Collections;

public class ChestMode: InventoryMode 
{
    private Control chestBack;
    private Label chestLabel;

    private Array<ItemIcon> chestButtons = new Array<ItemIcon>();

    public ChestMode (InventoryMenu menu)
    : base(menu)
    {
        chestBack = menu.GetNode<Control>("back/chestBack");
        chestLabel = chestBack.GetNode<Label>("Label");
    }

    public override void OpenMenu()
    {
        base.OpenMenu();
        chestBack.Visible = true;
    }

    public override void CloseMenu()
    {
        menu.EmitSignal(nameof(InventoryMenu.MenuIsClosed));
        chestBack.Visible = false;
        base.CloseMenu();
        menu.ChangeMode(NewInventoryMode.Usual);
    }

    protected override void CloseWithoutAnimating()
    {
        menu.EmitSignal(nameof(InventoryMenu.MenuIsClosed));
        chestBack.Visible = false;
        base.CloseWithoutAnimating();
        menu.ChangeMode(NewInventoryMode.Usual);
    }

    protected override void CheckDragItem()
    {
        string itemType = tempItemData["type"].ToString();

        foreach(ItemIcon otherButton in itemButtons) {
            Control buttonControl = otherButton as Control;
            if(tempButton != otherButton && checkMouseInButton(buttonControl)) {
                ChangeItemButtons(tempButton, otherButton);
                SetTempButton(otherButton, false);
                dragIcon.Texture = null;
            }
        }
    }

    public override void UpdateInput(InputEvent @event)
    {
        if (menu.isOpen && tempButton != null) {
            UpdateDragging(@event);
        }
    }
}