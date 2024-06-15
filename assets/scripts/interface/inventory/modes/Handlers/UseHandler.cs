using Godot;
using Godot.Collections;

public class UseHandler
{
    private const float CLEAR_BAG_DISTANCE = 5;
    
    private Player Player => Global.Get().player;
    private PlayerInventory Inventory => Player.Inventory;

    public bool ModalOpened;
    
    public ItemIcon weaponButton;
    public ItemIcon armorButton;
    public ItemIcon artifactButton;
    
    public IChest tempBag;

    private ItemIcon tempButton => mode.tempButton;
    private InventoryMenu menu;
    private InventoryMode mode;
    private BindsHandler bindsHandler;
    
    private Control modalRead;
    private Label noteName;
    private RichTextLabel noteText;
    private Label closeHint;

    private Control loadingIcon;
    private AnimationPlayer loadingAnim;

    public UseHandler(InventoryMenu menu, InventoryMode mode, BindsHandler bindsHandler)
    {
        this.mode = mode;
        this.menu = menu;
        this.bindsHandler = bindsHandler;

        loadingIcon = menu.GetNode<Control>("helper/back/loadingIcon");
        loadingAnim = loadingIcon.GetNode<AnimationPlayer>("anim");
        if (!loadingAnim.IsConnected("animation_finished", menu, nameof(InventoryMenu.IconAnimFinished)))
        {
            loadingAnim.Connect("animation_finished", menu, nameof(InventoryMenu.IconAnimFinished));
        }

        var wearBack = menu.GetNode<Control>("helper/back/wearBack");
        weaponButton   = wearBack.GetNode<ItemIcon>("weapon");
        armorButton    = wearBack.GetNode<ItemIcon>("armor");
        artifactButton = wearBack.GetNode<ItemIcon>("artifact");

        modalRead = mode.modalRead;
        noteName  = modalRead.GetNode<Label>("noteName");
        noteText  = modalRead.GetNode<RichTextLabel>("noteText");
        closeHint = modalRead.GetNode<Label>("closeHint");

        tempBag = null;
    }

    public void ShowLoadingIcon()
    {
        Vector2 iconPos = menu.GetGlobalMousePosition();
        iconPos.x += 10;
        iconPos.y += 10;
        loadingIcon.RectGlobalPosition = iconPos;
        loadingIcon.Visible = true;
        loadingAnim.Play("load");
    }

    public void HideLoadingIcon()
    {
        loadingIcon.Visible = false;
        loadingAnim.Stop();
    }
    
    public async void UseTempItem()
    {
        if (Player.Health <= 0) return;
        if (tempButton == null) return;
        
        Player.EmitSignal(nameof(Player.UseItem), tempButton.myItemCode);

        var itemType = (ItemType)mode.tempItemData["type"];
        if (Inventory.ItemIsUsable(itemType)) 
        {
            switch(itemType) 
            {
                case ItemType.note:
                    ReadTempNote();
                    break;
                
                case ItemType.weapon:
                    WearTempItem(weaponButton);
                    break;
                
                case ItemType.armor:
                    WearTempItem(armorButton);
                    break;
                
                case ItemType.artifact:
                    WearTempItem(artifactButton);
                    break;
                
                default:
                    string oldBind = tempButton.GetBindKey();
                    string itemCode = tempButton.myItemCode;
                    
                    Inventory.UseItem(mode.tempItemData);
                    mode.RemoveTempItem();

                    var tempDeletingItem = bindsHandler.TempDeletingIcon;
                    if (Object.IsInstanceValid(tempDeletingItem))
                    {
                        await menu.ToSignal(tempDeletingItem, nameof(BindIcon.IsDeleting));
                    }

                    //если использовался забинденный предмет
                    //биндим на тот же бинд такой же
                    bindsHandler.BindTheSameItem(oldBind, itemCode);
                    break;
            }
        }
        
        mode.SetTempButton(null);
    }

    public void CloseModal()
    {
        modalRead.Visible = false;
        ModalOpened = false;
    }
    
    public void WearTempItem(ItemIcon wearButton)
    {
        //если вещь надевается
        if (tempButton != wearButton)
        {
            if (Player.IsSitting) return;
            
            //если предмет нельзя надеть
            if (!Inventory.CheckCanWearItem(tempButton.myItemCode))
            {
                var itemData = ItemJSON.GetItemData(tempButton.myItemCode);
                Inventory.MessageCantWear(itemData["name"].ToString());
                return;
            }
            
            //если уже надета другая вещь
            if (wearButton.myItemCode != null) 
            {
                if (!CanTakeItemOff()) return;
                Inventory.UnwearItem(wearButton.myItemCode, false);
            }

            //если игрок берет оружие в режиме сундука из сундука
            //оружие, которое он до этого носил, должно остаться у него
            if (mode is ChestMode && !string.IsNullOrEmpty(wearButton.myItemCode))
            {
                if (menu.HasEmptyButton)
                {
                    ItemIcon otherButton = mode.FirstEmptyButton;
                    mode.ChangeItemButtons(wearButton, otherButton);
                    mode.ChangeItemButtons(tempButton, wearButton);
                }
                else
                {
                    menu.DropItem(wearButton.myItemCode);
                    bindsHandler.ClearBind(wearButton);
                
                    wearButton.SetItem(tempButton.myItemCode);
                    mode.RemoveItemFromButton(tempButton);
                }
            }
            else
            {
                mode.ChangeItemButtons(tempButton, wearButton);
            }

            Inventory.WearItem(wearButton.myItemCode);
        } 
        else //если вещь снимается 
        {
            UnwearItem(wearButton);
        }
    }

    public void ForceUnwearItem(ItemIcon item)
    {
        if (Player.Health <= 0) return;
        
        Player.EmitSignal(nameof(Player.UseItem), item.myItemCode);
        UnwearItem(item);
    }
    
    private void UnwearItem(ItemIcon button)
    {
        if (tempButton == null) mode.SetTempButton(button);
        
        if (!CanTakeItemOff()) return;

        ItemIcon otherButton = mode.FirstEmptyButton;
        Inventory.UnwearItem(button.myItemCode);

        //если в инвентаре есть место
        if (otherButton != null) mode.ChangeItemButtons(button, otherButton);
        else DropTempItem();

        mode.SetTempButton(null, false);
    }
    
    public bool CanTakeItemOff()
    {
        if (mode.tempItemData == null || mode.tempItemData.Count == 0) return false;
        
        var itemType = (ItemType)mode.tempItemData["type"];
        if (itemType != ItemType.artifact || Inventory.artifact == "") return true;
        
        var artifactData = ItemJSON.GetItemData(Inventory.artifact);
        if (!artifactData.Contains("cantUnwear")) return true;
        
        Inventory.MessageCantUnwear(artifactData["name"].ToString());
        return false;
    }
    
    public void DropTempItem() 
    {
        if (mode.tempItemData.Contains("questItem"))
        {
            Inventory.MessageCantDrop(mode.tempItemData["name"].ToString());
            return;
        }
        
        CheckTempBag();
        
        if (tempBag == null)
        {
            tempBag = mode.bagSpawner.SpawnItemBag();
        }

        if (tempButton.GetCount() > 0) 
        {
            tempBag.ChestHandler.AmmoCount.Add(tempButton.myItemCode, tempButton.GetCount());
        } 
        else 
        {
            tempBag.ChestHandler.ItemCodes.Add(tempButton.myItemCode);
        }

        if (mode.CheckMouseInButton(weaponButton)) 
        {
            Inventory.UnwearItem(weaponButton.myItemCode);
        }
        if (mode.CheckMouseInButton(armorButton)) 
        {
            Inventory.UnwearItem(armorButton.myItemCode);
        }
        if (mode.CheckMouseInButton(artifactButton)) 
        {
            Inventory.UnwearItem(artifactButton.myItemCode);
        }
        
        Player.CheckDropItem(tempButton.myItemCode);
        
        mode.RemoveTempItem();
    }

    private void CheckTempBag()
    {
        if (tempBag is Spatial bagPoint)
        {
            var distance = Player.GlobalTransform.origin.DistanceTo(bagPoint.GlobalTransform.origin);
            if (distance > CLEAR_BAG_DISTANCE)
            {
                tempBag = null;
                
            }
        }
        else tempBag = null;
    }
    
    private void ReadTempNote()
    {
        noteText.Text = "";

        string code = mode.tempItemData["text"].ToString();
        Array text = InterfaceLang.GetPhrasesAsArray("notes", code);
        
        noteName.Text = mode.tempItemData["name"].ToString();
        foreach (string line in text) 
        {
            noteText.Text += line + "\n";
        }

        string hintText = InterfaceLang.GetPhrase("inventory", "modalRead", "closeHint");
        closeHint.Text  = hintText.Replace("#button#", Global.GetKeyName("ui_focus_next"));
        
        tempButton._on_itemIcon_mouse_exited();
        modalRead.Visible = true;
        ModalOpened = true;
    }
}
