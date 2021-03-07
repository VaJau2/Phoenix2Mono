using Godot;
using Godot.Collections;

public class UsualMode: InventoryMode {

    private PackedScene bagPrefab;
    private FurnChest tempBag = null;
    private Control wearBack;

    private ItemIcon weaponButton;
    private ItemIcon armorButton;
    private ItemIcon artifactButton;

    private Label noteName;
    private RichTextLabel noteText;
    private Label closeHint;
    private bool isReading;

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

        bagPrefab = GD.Load<PackedScene>("res://objects/props/furniture/bag.tscn");
    }

    public override void OpenMenu()
    {
        base.OpenMenu();
        wearBack.Visible = true;
    }

    public override void CloseMenu()
    {
        if (isReading) {
            isReading = false;
            modalRead.Visible = false;
            return;
        }
        
        tempBag = null;
        wearBack.Visible = false;
        base.CloseMenu();
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
                        if (menu.bindedButtons.Keys.Contains(i)) {
                            //если нажата та же кнопка, она стирается
                            if (menu.bindedButtons[i] == tempButton) {
                                tempButton.SetBindKey(null);
                                menu.bindedButtons.Remove(i);
                            } else {
                            //если на ту же кнопку биндится другая кнопка, предыдущая стирается
                                ItemIcon oldBindedButton = menu.bindedButtons[i];
                                oldBindedButton.SetBindKey(null);
                                menu.bindedButtons[i] = tempButton;
                                tempButton.SetBindKey(i.ToString());
                            }
                        } else {
                            //если кнопка биндится впервые
                            menu.bindedButtons[i] = tempButton;
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
            if (Input.IsKeyPressed(48 + i) && menu.bindedButtons.Keys.Contains(i)) {
                SetTempButton(menu.bindedButtons[i], false);
                UseTempItem();
            }
        }
    }

    private void CheckAutoheal()
    {
        if (Input.IsActionJustPressed("autoheal") && player.MayMove) {
            if (player.Health == player.HealthMax) {
                inventory.ItemsMessage("youAreHealthy");
                return;
            }

            foreach(ItemIcon tempButton in itemButtons) {
                if (tempButton.myItemCode != null) {
                    SetTempButton(tempButton);
                    if (tempButton.myItemCode == "heal-potion" 
                    || tempItemData["type"].ToString() == "food") {
                        UseTempItem();
                        return;
                    }
                }
            }
            inventory.ItemsMessage("cantFindHeal");
            CheckTempIcon();
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
            inventory.UnwearItem(wearButton.myItemCode);

            //если в инвентаре есть место
            if (otherButton != null) {
                ChangeItemButtons(wearButton, otherButton);
            } else {
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
        foreach(string line in text) {
            noteText.Text += line + "\n";
        }

        string hintText = InterfaceLang.GetPhrase("inventory", "modalRead", "closeHint");
        closeHint.Text  = hintText.Replace("#button#", Global.GetKeyName("ui_focus_next"));
        
        tempButton._on_itemIcon_mouse_exited();
        modalRead.Visible = true;
        isReading = true;
    }

    protected void UseTempItem()
    {
        string itemType = tempItemData["type"].ToString();
        if (inventory.itemIsUsable(itemType)) {
            switch(itemType) {
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
                    inventory.UseItem(tempItemData);
                    RemoveTempItem();
                    break;
            }
        }
        tempButton = null;
    }

    protected void DropTempItem() 
    {
        if (tempBag == null) {
            tempBag = (FurnChest)bagPrefab.Instance();
            Node parent = player.GetNode("/root/Main/Scene");
            parent.AddChild(tempBag);
            tempBag.Translation = player.Translation;
            tempBag.Translate(Vector3.Down * 0.5f);
        }

        if (tempButton.GetCount() > 0) {
            tempBag.ammoCount.Add(tempButton.myItemCode, tempButton.GetCount());
        } else {
            tempBag.itemCodes.Add(tempButton.myItemCode);
        }
        
        RemoveTempItem();
    }

    public override void UpdateInput(InputEvent @event)
    {
        if (menu.isOpen && tempButton != null) {
            if (UpdateDragging(@event)) return;
            
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
            CheckAutoheal();
        }
    }
}