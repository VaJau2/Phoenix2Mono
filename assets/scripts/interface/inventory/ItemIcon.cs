using Godot;
using Godot.Collections;

public class ItemIcon : ColorRect
{
    private Control selected;
    private TextureRect icon;
    private InventoryMenu menu;

    public string myItemCode {get; private set;} = null;

    public int myItemNumber {get; private set;} = -1;

    public void SetItem(string itemCode, int itemNumber = -1)
    {
        myItemCode = itemCode;
        myItemNumber = itemNumber;
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        string path = "assets/textures/interface/icons/items/" + itemData["icon"] + ".png";
        StreamTexture newIcon = GD.Load<StreamTexture>(path);
        icon.Texture = newIcon;
    }

    public void ClearItem()
    {
        myItemCode = null;
        myItemNumber = -1;
        icon.Texture = null;
        _on_itemIcon_mouse_exited();
    }

    public override void _Ready()
    {
        selected = GetNode<Control>("selected");
        icon     = GetNode<TextureRect>("icon");
        menu     = GetNode<InventoryMenu>("../../../");
    }

    public void _on_itemIcon_mouse_entered()
    {
        if (myItemCode != null) {
            selected.Visible = true;
            icon.Modulate = Colors.Black;
            menu.SetTempIcon(this);
        }
    }

    public void _on_itemIcon_mouse_exited()
    {
        if (selected.Visible) {
            selected.Visible = false;
            icon.Modulate = Colors.White;
            menu.SetTempIcon(null);
        }
    }
}
