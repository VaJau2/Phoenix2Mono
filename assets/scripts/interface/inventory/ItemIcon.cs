using Godot;
using Godot.Collections;

public class ItemIcon : ColorRect
{
    Global global => Global.Get();
    Player player => global.player;

    [Export]
    public bool isInventoryIcon = false;
    private Control selected;
    private TextureRect icon;
    private Label bindLabel;
    private Label countLabel;
    private InventoryMenu menu;

    public string myItemCode {get; private set;} = null;

    public StreamTexture GetIcon() => (StreamTexture)icon.Texture;

    public virtual void SetIcon(StreamTexture newIcon) => icon.Texture = newIcon;

    public int GetCount() => int.Parse(countLabel.Text);

    //если IsAmmo - false, то при нуле кнопка не очищается
    public void SetCount(int count = 0, bool IsAmmo = true) 
    { 
        if (count > 0) 
        {
            countLabel.Text = count.ToString();
        } 
        else 
        {
            if (IsAmmo) 
            {
                ClearItem();
            } 
            else 
            {
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
        
        var itemType = (ItemType)itemData["type"];
        countLabel.Visible = (itemType == ItemType.ammo) || (itemType == ItemType.money);
        
        if (itemType == ItemType.ammo) 
        {
            if (isInventoryIcon) 
            {
                player.Inventory.SetAmmoButton(itemCode, this);
                
                //обновляем интерфейс, если новые патроны добавились для текущего оружия
                if (player.Weapons.IsTempAmmo(itemCode)) 
                {
                    player.Weapons.LoadNewAmmo();
                }
            }
        } 
        else if (itemType != ItemType.money) 
        {
            countLabel.Text = "-1";
        }

        if (isInventoryIcon)
        {
            player.EmitSignal(nameof(Player.TakeItem), itemCode);
        }
    }

    public void SetItemCode(string itemCode)
    {
        myItemCode = itemCode;
    }

    public void ClearItem()
    {
        //если очищается инвентарная иконка с патронами
        //ссылка на патроны также должна очиститься
        if (isInventoryIcon) 
        {
            Dictionary itemData = ItemJSON.GetItemData(myItemCode);
            var itemType = (ItemType)itemData["type"];
            
            if (itemType == ItemType.ammo) 
            {
                player.Inventory.ammoButtons.Remove(myItemCode);
                
                if (player.Weapons.IsTempAmmo(myItemCode)) 
                {
                    player.Weapons.LoadNewAmmo();
                }
            }
        }

        SetBindKey(null); 
        myItemCode   = null;
        icon.Texture = null;
        countLabel.Text = "-1";
        countLabel.Visible = false;
        _on_itemIcon_mouse_exited();
    }

    public void SetBindKey(string text)
    {
        bindLabel.Text = text;
    }

    public string GetBindKey() 
    {
        return bindLabel.Text;
    }

    private bool MayShowInfo()
    {
        return !menu.mode.isDragging
        && !menu.mode.modalAsk.Visible
        && !menu.mode.modalRead.Visible;
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
        if (myItemCode != null && MayShowInfo()) {
            selected.Visible = true;
            icon.Modulate = Colors.Black;
            menu.SetTempButton(this);
        }
    }

    public void _on_itemIcon_mouse_exited()
    {
        if (selected.Visible && MayShowInfo()) {
            selected.Visible = false;
            icon.Modulate = global.Settings.interfaceColor;
            menu.SetTempButton(null);
        }
    }
}
