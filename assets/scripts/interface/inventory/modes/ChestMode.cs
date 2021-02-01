using Godot;
using Godot.Collections;

public class ChestMode: InventoryMode 
{
    private Control chestBack;
    private Label chestLabel;

    private Array<ItemIcon> chestButtons = new Array<ItemIcon>();

    private FurnChest chest;

    public ChestMode (InventoryMenu menu)
    : base(menu)
    {
        chestBack = menu.GetNode<Control>("back/chestBack");
        chestLabel = chestBack.GetNode<Label>("Label");
        foreach(object button in chestBack.GetNode<Control>("items").GetChildren()) {
            chestButtons.Add(button as ItemIcon); 
        }
    }

    public void SetChest(FurnChest chest)
    {
        this.chest = chest;
        string chestName = InterfaceLang.GetPhrase("inGame", "chestNames", chest.chestCode);
        chestLabel.Text = chestName;
        if(chest.itemPositions.Count > 0) {
            LoadChestButtons(chest.itemPositions, chest.ammoCount);
        } else {
            LoadChestButtons(chest.itemCodes, chest.ammoCount);
        }
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

    public override void UpdateInput(InputEvent @event)
    {
        if (menu.isOpen && tempButton != null) {
            if (UpdateDragging(@event)) return;
            if (Input.IsActionJustReleased("ui_click")) {
                TakeTempItem();
            }
        }
    }

    private void ClearChestButtons()
    {
        foreach(ItemIcon tempIcon in chestButtons) {
            tempIcon.ClearItem();
        }
    }

    //загружаем предметы, если сундук открывается впервые
    private void LoadChestButtons(Array<string> newItems, Dictionary<string, int> ammo)
    {
        ClearChestButtons();
        for (int i = 0; i < newItems.Count; i++) {
            AddChestItem(newItems[i]);
        }
        foreach(string ammoItem in ammo.Keys) {
            ItemIcon newAmmoButton = AddChestItem(ammoItem);
            newAmmoButton.SetCount(ammo[ammoItem]);
            chest.ammoButtons.Add(ammoItem, newAmmoButton);
        }
    }

    //загружаем предметы, если сундук уже открывался
    private void LoadChestButtons(Dictionary<int, string> itemPositions, Dictionary<string, int> ammo)
    {
        ClearChestButtons();
        //в массиве itemPositions также лежат патроны
        foreach(int buttonId in itemPositions.Keys) {
            string itemCode = itemPositions[buttonId];
            ItemIcon tempButton = chestButtons[buttonId];
            tempButton.SetItem(itemCode);

            //грузим количество этих патронов
            if(ammo.ContainsKey(itemCode)) {
                tempButton.SetCount(ammo[itemCode]);
            }
        }
    }

    private ItemIcon FirstEmptyChestButton 
    {
        get {
            foreach(ItemIcon button in chestButtons) {
                if (button.myItemCode == null) {
                    return button;
                }
            }
            return null;
        }
    }

    private ItemIcon AddChestItem(string itemCode) 
    {
        ItemIcon emptyButton = FirstEmptyChestButton;
        if (emptyButton != null) {
            emptyButton.SetItem(itemCode);
            
            int buttonId = chestButtons.IndexOf(emptyButton);
            chest.itemPositions.Add(buttonId, itemCode);
        } else {
            inventory.MessageNotEnoughSpace();
        }
        
        return emptyButton;
    }

    //сохраняем в сундук позиции кнопок
    private void UpdateChestPositions() 
    {
        //очищаем все массивы сундука
        chest.itemPositions.Clear();
        chest.ammoCount.Clear();
        chest.ammoButtons.Clear();

        //проходим по иконкам
        foreach(ItemIcon tempIcon in chestButtons) {
            if(tempIcon.myItemCode != null) {
                //сохраняем позицию иконки
                int iconId = chestButtons.IndexOf(tempIcon);
                chest.itemPositions.Add(iconId, tempIcon.myItemCode);
                //сохраняем количество, если это патроны
                if(tempIcon.GetCount() != -1) {
                    chest.ammoCount.Add(tempIcon.myItemCode, tempIcon.GetCount());
                    chest.ammoButtons.Add(tempIcon.myItemCode, tempIcon);
                }
            }
        }
    }

    protected override void CloseWithoutAnimating()
    {
        menu.EmitSignal(nameof(InventoryMenu.MenuIsClosed));
        chestBack.Visible = false;
        base.CloseWithoutAnimating();
        menu.ChangeMode(NewInventoryMode.Usual);
    }

    private bool CheckDragIn(Array<ItemIcon> iconsArray, string ammoName)
    {
        foreach(ItemIcon otherButton in iconsArray) {
            Control buttonControl = otherButton as Control;
            if(tempButton != otherButton && checkMouseInButton(buttonControl)) {
                switch (ammoName) {
                    case "chest":
                        if (checkAmmoInChest()) return true;
                        break;
                    case "inventory":
                        if (checkAmmoInInventory()) return true;
                        break;
                }

                ChangeItemButtons(tempButton, otherButton);
                SetTempButton(otherButton, false);
                dragIcon.Texture = null;
                UpdateChestPositions();
                return true;
            }
        }
        return false;
    }

    protected override void CheckDragItem()
    {
        if (CheckDragIn(itemButtons, "inventory")) return;
        if (CheckDragIn(chestButtons, "chest"))    return;
    }

    protected override void LoadControlHint(bool isInventoryIcon)
    {
        string phraseName = isInventoryIcon ? "put" : "take";
        controlHints.Text = InterfaceLang.GetPhrase(
            "inventory", 
            "chestControlHints", 
            phraseName
        );
    }

    protected override void ChangeItemButtons(ItemIcon oldButton, ItemIcon newButton)
    {
        oldButton.SetBindKey(null);
        base.ChangeItemButtons(oldButton, newButton);
    }

    private void TakeTempItem()
    {
        //положить в сундук
        if (itemButtons.Contains(tempButton)) {
            ItemIcon chestButton = FirstEmptyChestButton;
            if (chestButton != null) {
                if (checkAmmoInChest()) return;
                ChangeItemButtons(tempButton, chestButton);
                SetTempButton(chestButton, false);
                UpdateChestPositions();
            } else {
                inventory.MessageNotEnoughSpace();
            }
            return;
        }

        //взять из сундука
        if(chestButtons.Contains(tempButton)) {
            ItemIcon itemButton = FirstEmptyButton;
            if (itemButton != null) {
                if (checkAmmoInInventory()) return;
                ChangeItemButtons(tempButton, itemButton);
                SetTempButton(itemButton, false);
                UpdateChestPositions();
            } else {
                inventory.MessageNotEnoughSpace();
            }
        }
    }

    //проверяем, есть ли в сундуке патроны, которые собираемся ложить
    private bool checkAmmoInChest()
    {
        if (chestButtons.Contains(tempButton)) return false;

        if (chest.ammoCount.Keys.Contains(tempButton.myItemCode)) {
            ItemIcon ammoButton = chest.ammoButtons[tempButton.myItemCode];
            int addCount = tempButton.GetCount();
            ammoButton.SetCount(ammoButton.GetCount() + addCount);
            tempButton.ClearItem();
            UpdateChestPositions();
            return true;
        }
        return false;
    }

    //проверяем, есть ли в инвентаре патроны, которые собираемся ложить
    private bool checkAmmoInInventory()
    {
        if (itemButtons.Contains(tempButton)) return false;

        if (inventory.ammoButtons.Keys.Contains(tempButton.myItemCode)) {
            ItemIcon ammoButton = inventory.ammoButtons[tempButton.myItemCode];
            int addCount = tempButton.GetCount();
            ammoButton.SetCount(ammoButton.GetCount() + addCount);
            tempButton.ClearItem();
            UpdateChestPositions();

            if (player.Weapons.tempAmmoButton == ammoButton) {
                player.Weapons.UpdateAmmoCount();
            }

            return true;
        }
        return false;
    }
}