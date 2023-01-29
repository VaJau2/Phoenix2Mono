using System;
using Godot;
using Array = Godot.Collections.Array;
using Object = Godot.Object;

public class UsualMode: InventoryMode 
{
    private FurnChest tempBag;
    private Control wearBack;

    private ItemIcon weaponButton;
    private ItemIcon armorButton;
    private ItemIcon artifactButton;

    private Label noteName;
    private RichTextLabel noteText;
    private Label closeHint;

    private bool countClickTimer;
    private float clickTimer;

    public UsualMode (InventoryMenu menu)
    : base(menu)
    {
        wearBack       = back.GetNode<Control>("wearBack");
        weaponButton   = wearBack.GetNode<ItemIcon>("weapon");
        armorButton    = wearBack.GetNode<ItemIcon>("armor");
        artifactButton = wearBack.GetNode<ItemIcon>("artifact");

        noteName       = modalRead.GetNode<Label>("noteName");
        noteText       = modalRead.GetNode<RichTextLabel>("noteText");
        closeHint      = modalRead.GetNode<Label>("closeHint");
    }

    public void SetUseBindsCooldown(float cooldown)
    {
        bindsHandler.useCooldown = cooldown;
    }

    public override void OpenMenu()
    {
        base.OpenMenu();
        wearBack.Visible = true;
    }

    public override void CloseModal()
    {
        modalRead.Visible = false;
        ModalOpened = false;
    }

    public override void CloseMenu()
    {
        CloseModal();
        tempBag = null;
        wearBack.Visible = false;
        base.CloseMenu();
    }

    private bool IsUnwearingItem(string itemType)
    {
        return (itemType == "weapon" && tempButton == weaponButton)
        || (itemType == "armor" && tempButton == armorButton)
        || (itemType == "artifact" && tempButton == artifactButton);
    }

    protected override void CheckDragItem()
    {
        string itemType = tempItemData["type"].ToString();
        
        switch (itemType)
        {
            case "weapon" when CheckMouseInButton(weaponButton):
                WearTempItem(weaponButton); return;
            case "armor" when CheckMouseInButton(armorButton):
                WearTempItem(armorButton); return;
            case "artifact" when CheckMouseInButton(artifactButton):
                WearTempItem(artifactButton); return;
        }

        foreach (var otherButton in itemButtons) 
        {
            var buttonControl = (Control) otherButton;
            if (tempButton == otherButton || !CheckMouseInButton(buttonControl)) continue;
            if (IsUnwearingItem(itemType)) 
            {
                if (!CanTakeItemOff()) return;
                inventory.UnwearItem(tempButton.myItemCode);
            }

            ChangeItemButtons(tempButton, otherButton);
            SetTempButton(otherButton, false);
            dragIcon.Texture = null;
        }
    }

    private void UseHotkeys() 
    {
        if (bindsHandler.useCooldown > 0) return;

        for (var i = 0; i < 10; i++)
        {
            if (!Input.IsKeyPressed(48 + i) || !menu.bindedButtons.Keys.Contains(i)) continue;
            SetTempButton(menu.bindedButtons[i], false);
            UseTempItem();
        }
    }

    private void CheckAutoheal()
    {
        if (!Object.IsInstanceValid(player)) return;
        if (!Input.IsActionJustPressed("autoheal") || !player.MayMove) return;
        if (player.Health == player.HealthMax) 
        {
            inventory.ItemsMessage("youAreHealthy");
            return;
        }

        foreach (var newTempButton in itemButtons)
        {
            if (newTempButton.myItemCode == null) continue;
            SetTempButton(newTempButton);
            if (tempButton.myItemCode != "heal-potion" && tempItemData["type"].ToString() != "food") continue;
            UseTempItem();
            return;
        }
        inventory.ItemsMessage("cantFindHeal");
        CheckTempIcon();
    }

    private bool CanTakeItemOff() 
    {
        string itemType = tempItemData["type"].ToString();
        if (itemType != "artifact" || inventory.artifact == "") return true;
        
        var artifactData = ItemJSON.GetItemData(inventory.artifact);
        if (!artifactData.Contains("cantUnwear")) return true;
        inventory.MessageCantUnwear(artifactData["name"].ToString());
        return false;
    }

    private void WearTempItem(ItemIcon wearButton)
    {
        //если вещь надевается
        if (tempButton != wearButton)
        {
            if (player.IsSitting) return;
            
            //если предмет нельзя надеть
            if (!inventory.CheckCanWearItem(tempButton.myItemCode))
            {
                var itemData = ItemJSON.GetItemData(tempButton.myItemCode);
                inventory.MessageCantWear(itemData["name"].ToString());
                return;
            }
            
            //если уже надета другая вещь
            if (wearButton.myItemCode != null) 
            {
                if (!CanTakeItemOff()) return;
                inventory.UnwearItem(wearButton.myItemCode, false);
            }
            ChangeItemButtons(tempButton, wearButton);
            inventory.WearItem(wearButton.myItemCode);
        } //если вещь снимается 
        else 
        {
            if (!CanTakeItemOff()) return;

            ItemIcon otherButton = FirstEmptyButton;
            inventory.UnwearItem(wearButton.myItemCode);

            //если в инвентаре есть место
            if (otherButton != null) 
            {
                ChangeItemButtons(wearButton, otherButton);
            } 
            else 
            {
                DropTempItem();
            }
        }
    }

    private void ReadTempNote()
    {
        noteText.Text = "";

        string code = tempItemData["text"].ToString();
        Array text = InterfaceLang.GetPhrasesAsArray("notes", code);
        
        noteName.Text = tempItemData["name"].ToString();
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

    private void UseTempItem()
    {
        if (player.Health <= 0) return;
        
        player.EmitSignal(nameof(Player.UseItem), tempButton.myItemCode);
        
        string itemType = tempItemData["type"].ToString();
        if (inventory.itemIsUsable(itemType)) 
        {
            switch(itemType) 
            {
                case "note":
                    ReadTempNote();
                    break;
                case "weapon":
                    WearTempItem(weaponButton);
                    break;
                case "armor":
                    WearTempItem(armorButton);
                    break;
                case "artifact":
                    WearTempItem(artifactButton);
                    break;
                default:
                    string oldBind = tempButton.GetBindKey();
                    string itemCode = tempButton.myItemCode;
                    
                    inventory.UseItem(tempItemData);
                    RemoveTempItem();

                    //если использовался забинденный предмет
                    //биндим на тот же бинд такой же
                    bindsHandler.BindTheSameItem(oldBind, itemCode);
                    break;
            }
        }
        SetTempButton(null);
    }

    private void DropTempItem() 
    {
        if (tempItemData.Contains("questItem"))
        {
            inventory.MessageCantDrop(tempItemData["name"].ToString());
            return;
        }

        if (tempBag == null)
        {
            tempBag = SpawnItemBag();
        }

        if (tempButton.GetCount() > 0) 
        {
            tempBag.ammoCount.Add(tempButton.myItemCode, tempButton.GetCount());
        } else 
        {
            tempBag.itemCodes.Add(tempButton.myItemCode);
        }

        if (CheckMouseInButton(weaponButton)) 
        {
            inventory.UnwearItem(weaponButton.myItemCode);
        }
        if (CheckMouseInButton(armorButton)) 
        {
            inventory.UnwearItem(armorButton.myItemCode);
        }
        if (CheckMouseInButton(artifactButton)) 
        {
            inventory.UnwearItem(artifactButton.myItemCode);
        }
        
        RemoveTempItem();
    }

    public override void Process(float delta)
    {
        countClickTimer = false;
        if (menu.isOpen && tempButton != null)
        {
            if (Input.IsActionPressed("ui_click"))
            {
                countClickTimer = true;
            }
        }

        if (countClickTimer)
        {
            if (clickTimer < 0.5f)
            {
                clickTimer += delta;
            }
        }
        else
        {
            clickTimer = 0;
        }

        bindsHandler.UpdateUseCooldown(delta);
    }

    public override void UpdateInput(InputEvent @event)
    {
        if (menu.isOpen && tempButton != null) 
        {
            if (UpdateDragging(@event)) return;
            
            if (Input.IsActionJustReleased("ui_click") && clickTimer < 0.2f) 
            {
                UseTempItem();
            }

            if (Input.IsMouseButtonPressed(2) && !isDragging && tempButton != null) 
            {
                DropTempItem();
            }

            if (@event is InputEventKey) 
            {
                bindsHandler.BindHotkeys(tempItemData["type"].ToString());
            }
        }
        
        if (@event is InputEventKey && tempButton == null) 
        {
            UseHotkeys();
            CheckAutoheal();
        }
    }
}