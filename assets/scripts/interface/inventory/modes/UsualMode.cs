using Godot;
using Godot.Collections;

public class UsualMode: InventoryMode {

    private Control wearBack;

    private ItemIcon weaponButton;
    private ItemIcon armorButton;
    private ItemIcon artifactButton;

    public UsualMode (InventoryMenu menu)
    : base(menu)
    {
        wearBack       = menu.GetNode<Control>("back/wearBack");
        weaponButton   = wearBack.GetNode<ItemIcon>("weapon");
        armorButton    = wearBack.GetNode<ItemIcon>("armor");
        artifactButton = wearBack.GetNode<ItemIcon>("artifact");
    }

    public override void OpenMenu()
    {
        base.OpenMenu();
        wearBack.Visible = true;
    }

    public override void CloseMenu()
    {
        wearBack.Visible = false;
        base.CloseMenu();
    }

    protected override void CloseWithoutAnimating()
    {
        wearBack.Visible = false;
        base.CloseWithoutAnimating();
    }

    private bool isUnwearingItem(string itemType)
    {
        return (itemType == "weapon" && tempButton == weaponButton)
        || (itemType == "armor" && tempButton == armorButton)
        || (itemType == "artifact" && tempButton == artifactButton);
    }

    protected override void CheckDragItem()
    {
        string itemType = tempItemData["type"].ToString();
        
        if (itemType == "weapon" && checkMouseInButton(weaponButton)) {
            WearTempItem(weaponButton); return;
        }
        if (itemType == "armor" && checkMouseInButton(armorButton)) {
            WearTempItem(armorButton); return;
        }
        if (itemType == "artifact" && checkMouseInButton(artifactButton)) {
            WearTempItem(artifactButton); return;
        }

        foreach(ItemIcon otherButton in itemButtons) {
            Control buttonControl = otherButton as Control;
            if(tempButton != otherButton && checkMouseInButton(buttonControl)) {
                if(isUnwearingItem(itemType)) {
                    if (!canTakeItemOff()) return;
                    inventory.UnwearItem(tempButton.myItemCode);
                }

                ChangeItemButtons(tempButton, otherButton);
                SetTempButton(otherButton, false);
                dragIcon.Texture = null;
            }
        }
    }

    protected bool ItemIsBindable(string itemType) 
    {
        return itemType == "weapon" || itemType == "food" || itemType == "meds";
    }

    protected void BindHotkeys() 
    {
        if (tempButton.myItemCode != null) {
            if (ItemIsBindable(tempItemData["type"].ToString())) {
                for (int i = 0; i < 10; i++) {
                    if (Input.IsKeyPressed(48 + i)) {
                        //если клавиша уже забиндена
                        if (bindedButtons.Keys.Contains(i)) {
                            //если нажата та же кнопка, она стирается
                            if (bindedButtons[i] == tempButton) {
                                tempButton.SetBindKey(null);
                                bindedButtons.Remove(i);
                            } else {
                            //если на ту же кнопку биндится другая кнопка, предыдущая стирается
                                ItemIcon oldBindedButton = bindedButtons[i];
                                oldBindedButton.SetBindKey(null);
                                bindedButtons[i] = tempButton;
                                tempButton.SetBindKey(i.ToString());
                            }
                        } else {
                            //если кнопка биндится впервые
                            bindedButtons[i] = tempButton;
                            tempButton.SetBindKey(i.ToString());
                        }
                    }
                }
            }
        }
    }

    protected void UseHotkeys() 
    {
        for (int i = 0; i < 10; i++) {
            if (Input.IsKeyPressed(48 + i) && bindedButtons.Keys.Contains(i)) {
                SetTempButton(bindedButtons[i], false);
                UseTempItem();
            }
        }
    }

    protected bool canTakeItemOff() 
    {
        string itemType = tempItemData["type"].ToString();
        if (itemType == "artifact" && inventory.artifact != "") {
            Dictionary artifactData = ItemJSON.GetItemData(inventory.artifact);
            if (artifactData.Contains("cantUnwear")) {
                inventory.MessageCantUnwear(artifactData["name"].ToString());
                return false;
            }
        }
        
        return true;
    }

    protected void WearTempItem(ItemIcon wearButton)
    {
        //если вещь надевается
        if (tempButton != wearButton) {
            //если уже надета другая вещь
            if (wearButton.myItemCode != null) {
                if (!canTakeItemOff()) return;
                inventory.UnwearItem(wearButton.myItemCode, false);
            }
            ChangeItemButtons(tempButton, wearButton);
            inventory.WearItem(wearButton.myItemCode);
        } //если вещь снимается 
        else {
            if (!canTakeItemOff()) return;

            ItemIcon otherButton = FirstEmptyButton;
            //если в инвентаре есть место
            if (otherButton != null) {
                inventory.UnwearItem(wearButton.myItemCode);
                ChangeItemButtons(wearButton, otherButton);
            } else {
                DropTempItem();
            }
        }
    }

    protected void UseTempItem()
    {
        string itemType = tempItemData["type"].ToString();
        if (inventory.itemIsUsable(itemType)) {
            switch(itemType) {
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
                    inventory.UseItem(tempItemData);
                    RemoveTempItem();
                    break;
            }
        }
        tempButton = null;
    }

    protected void DropTempItem() 
    {
        RemoveTempItem();
    }

    public override void UpdateInput(InputEvent @event)
    {
        if (menu.isOpen && tempButton != null) {
            UpdateDragging(@event);
            
            if (Input.IsActionJustReleased("ui_click")) {
                UseTempItem();
            }

            if (Input.IsMouseButtonPressed(2) && !isDragging && tempButton != null) {
                DropTempItem();
            }

            if (@event is InputEventKey) {
                BindHotkeys();
            }
        }
        if(@event is InputEventKey && tempButton == null) {
            UseHotkeys();
        }
    }
}