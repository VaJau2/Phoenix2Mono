using Godot;
using Godot.Collections;

public class ItemIcon : ColorRect
{
    [Export]
    public bool isInventoryIcon = false;
    private Control selected;
    private TextureRect icon;
    private Label bindLabel;
    private Label countLabel;
    private InventoryMenu menu;
    private Color lastColor;

    public string myItemCode {get; private set;} = null;

    public StreamTexture GetIcon() => (StreamTexture)icon.Texture;

    public void SetIcon(StreamTexture newIcon) => icon.Texture = newIcon;

    public int GetCount() => int.Parse(countLabel.Text);

    //если IsAmmo - false, то при нуле кнопка не очищается
    public void SetCount(int count = 0, bool IsAmmo = true) 
    { 
        if (count > 0) {
            countLabel.Text = count.ToString();
        } else {
            if (IsAmmo) {
                ClearItem();
            } else {
                countLabel.Visible = false;
            } 
        }
    }

    public void SetItem(string itemCode)
    {
        myItemCode = itemCode;
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        string path = "assets/textures/interface/icons/items/" + itemData["icon"] + ".png";
        StreamTexture newIcon = GD.Load<StreamTexture>(path);
        SetIcon(newIcon);
        
        string itemType = itemData["type"].ToString();
        countLabel.Visible = (itemType == "ammo") || (itemType == "money");
        if (itemType == "ammo") {
            if (isInventoryIcon) {
                Player player = Global.Get().player;
                player.inventory.SetAmmoButton(itemCode, this);
                //обновляем интерфейс, если новые патроны добавились для текущего оружия
                if (player.Weapons.isTempAmmo(itemCode)) {
                    player.Weapons.LoadNewAmmo();
                }
            }
        } else {
            countLabel.Text = "-1";
        }
    }

    public void ClearItem()
    {
        //если очищается инвентарная иконка с патронами
        //ссылка на патроны также должна очиститься
        if (isInventoryIcon) {
            Dictionary itemData = ItemJSON.GetItemData(myItemCode);
            string itemType = itemData["type"].ToString();
            if (itemType == "ammo") {
                Player player = Global.Get().player;
                player.inventory.ammoButtons.Remove(myItemCode);
                if (player.Weapons.isTempAmmo(myItemCode)) {
                    player.Weapons.LoadNewAmmo();
                }
            }
        }

        myItemCode   = null;
        icon.Texture = null;
        SetBindKey("");
        countLabel.Text = "-1";
        countLabel.Visible = false;
        _on_itemIcon_mouse_exited();
    }

    public void SetBindKey(string text) {
        bindLabel.Text = text;
    }

    public string GetBindKey() {
        return bindLabel.Text;
    }

    public override void _Ready()
    {
        selected = GetNode<Control>("selected");
        icon     = GetNode<TextureRect>("icon");
        menu     = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        bindLabel  = GetNode<Label>("bindLabel");
        countLabel = GetNode<Label>("countLabel");
    }

    public void _on_itemIcon_mouse_entered()
    {
        if (myItemCode != null 
        && !menu.mode.isDragging
        && !menu.mode.modalAsk.Visible) {
            selected.Visible = true;
            lastColor = icon.Modulate;
            icon.Modulate = Colors.Black;
            menu.SetTempButton(this);
        }
    }

    public void _on_itemIcon_mouse_exited()
    {
        if (selected.Visible 
        && !menu.mode.isDragging 
        && !menu.mode.modalAsk.Visible) {
            selected.Visible = false;
            icon.Modulate = lastColor;
            menu.SetTempButton(null);
        }
    }
}
