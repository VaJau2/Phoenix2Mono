using Godot;
using Godot.Collections;

public class ItemIcon : ColorRect
{
    private Control selected;
    private TextureRect icon;
    private Label bindLabel;
    private InventoryMenu menu;

    public string myItemCode = null;

    public StreamTexture GetIcon() 
    {
        return (StreamTexture)icon.Texture;
    }

    public void SetIcon(StreamTexture newIcon) => icon.Texture = newIcon;


    public void SetItem(string itemCode)
    {
        myItemCode = itemCode;
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        string path = "assets/textures/interface/icons/items/" + itemData["icon"] + ".png";
        StreamTexture newIcon = GD.Load<StreamTexture>(path);
        SetIcon(newIcon);
    }

    public void ClearItem()
    {
        myItemCode = null;
        icon.Texture = null;
        _on_itemIcon_mouse_exited();
        SetBindKey(null);
    }

    public void SetBindKey(string text) {
        bindLabel.Text = text;
    }

    public string GetBindKey() {
        return bindLabel.Text;
    }

    public override void _Ready()
    {
        selected  = GetNode<Control>("selected");
        icon      = GetNode<TextureRect>("icon");
        menu      = GetNode<InventoryMenu>("../../../");
        bindLabel = GetNode<Label>("bindLabel");
    }

    public void _on_itemIcon_mouse_entered()
    {
        if (myItemCode != null && !menu.isDragging) {
            selected.Visible = true;
            icon.Modulate = Colors.Black;
            menu.SetTempButton(this);
        }
    }

    public void _on_itemIcon_mouse_exited()
    {
        if (selected.Visible && !menu.isDragging) {
            selected.Visible = false;
            icon.Modulate = Colors.White;
            menu.SetTempButton(null);
        }
    }
}
