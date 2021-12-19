using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Object = Godot.Object;

public class ChestMode: InventoryMode 
{
    private Control chestBack;
    private Label chestLabel;
    private Button takeAll;

    private Array<ItemIcon> chestButtons = new Array<ItemIcon>();

    private FurnChest tempChest;

    public ChestMode (InventoryMenu menu)
    : base(menu)
    {
        chestBack  = back.GetNode<Control>("chestBack");
        chestLabel = chestBack.GetNode<Label>("Label");
        takeAll    = chestBack.GetNode<Button>("takeAll");
        foreach(object button in chestBack.GetNode<Control>("items").GetChildren()) {
            chestButtons.Add(button as ItemIcon); 
        }
    }

    public void SetChest(FurnChest chest)
    {
        this.tempChest = chest;
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

        string askTakeAllText = InterfaceLang.GetPhrase("inventory", "labels", "takeAll");
        takeAll.Text = askTakeAllText.Replace("#button#", Global.GetKeyName("ui_shift"));
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
        if (menu.isOpen) {
            if (tempButton != null) {
                if (UpdateDragging(@event)) return;
                if (Input.IsActionJustReleased("ui_click")) {
                    TakeTempItem();
                }
            }
            if (Input.IsActionJustPressed("ui_shift")) {
                _on_takeAll_pressed();
            }

            if (Input.IsActionJustPressed("use"))
            {
                MenuManager.CloseMenu(menu);
            }
        }
    }

    public override async void _on_takeAll_pressed()
    {
        //чтоб щит не включался на ту же кнопку
        if (player is Player_Unicorn unicorn)
        {
            unicorn.shield.shieldCooldown = 0.5f;
        }
        
        foreach(ItemIcon tempIcon in chestButtons) {
            if (tempIcon.myItemCode != null) {
                SetTempButton(tempIcon);
                if (!TakeTempItem()) return;
            }
        }
        await Global.Get().ToTimer(0.1f, null, true);
        if (Object.IsInstanceValid(tempChest)) UpdateChestPositions();
        MenuManager.CloseMenu(menu);
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
            tempChest.ammoButtons.Add(ammoItem, newAmmoButton);
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
            CheckAddMoney(tempButton, itemCode);

            //грузим количество этих патронов
            if(ammo.ContainsKey(itemCode)) {
                tempButton.SetCount(ammo[itemCode]);
            }
        }
    }

    private ItemIcon FirstEmptyChestButton => chestButtons.FirstOrDefault(button => button.myItemCode == null);

    private ItemIcon AddChestItem(string itemCode) 
    {
        ItemIcon emptyButton = FirstEmptyChestButton;
        if (emptyButton != null) {
            CheckAddMoney(emptyButton, itemCode);
            
            int buttonId = chestButtons.IndexOf(emptyButton);
            if (!tempChest.itemPositions.ContainsKey(buttonId)) {
                tempChest.itemPositions.Add(buttonId, itemCode);
            }
            
        } else {
            inventory.ItemsMessage("space");
        }
        
        return emptyButton;
    }

    private void CheckAddMoney(ItemIcon button, string itemCode)
    {
        button.SetItem(itemCode);

        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        if (itemData["type"].ToString() == "money") {
            button.SetCount(tempChest.moneyCount);
        }
    }

    //сохраняем в сундук позиции кнопок
    private void UpdateChestPositions() 
    {
        //очищаем все массивы сундука
        tempChest.itemCodes.Clear();
        tempChest.itemPositions.Clear();
        tempChest.ammoCount.Clear();
        tempChest.ammoButtons.Clear();
        bool isEmpty = true;

        //проходим по иконкам
        foreach(ItemIcon tempIcon in chestButtons) {
            if(tempIcon.myItemCode != null) {
                isEmpty = false; //если есть вещи, то сундук не пустой

                //сохраняем позицию иконки
                int iconId = chestButtons.IndexOf(tempIcon);
                tempChest.itemPositions.Add(iconId, tempIcon.myItemCode);
                //сохраняем количество, если это патроны
                if(tempIcon.GetCount() != -1) {
                    tempChest.ammoCount.Add(tempIcon.myItemCode, tempIcon.GetCount());
                    tempChest.ammoButtons.Add(tempIcon.myItemCode, tempIcon);
                }
            }
        }

        //если сундук - это сумка, и она опустошается
        //то она удаляется
        if (isEmpty && tempChest.isBag) { 
            Global.AddDeletedObject(tempChest.Name);
            tempChest.QueueFree();
            MenuManager.CloseMenu(menu);
        }
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
                SetTempButton(null, false);
                dragIcon.Texture = null;
                UpdateChestPositions();
                return true;
            }
        }
        return false;
    }

    protected override void CheckDragItem()
    {
        //перетащить из сундука
        if (CheckDragIn(itemButtons, "inventory")) return;

        if (tempItemData.Contains("questItem"))
        {
            inventory.MessageCantDrop(tempItemData["name"].ToString());
            return;
        }
        //перетащить в сундук
        if (CheckDragIn(chestButtons, "chest"))    return;
    }

    //грузим подсказки по управлению предметом
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
        if (!IconsInSameArray(oldButton, newButton) && !string.IsNullOrEmpty(oldButton.GetBindKey()))
        {
            ClearBind(oldButton);
        }
        base.ChangeItemButtons(oldButton, newButton);
    }

    private bool TakeTempItem()
    {
        //положить в сундук
        if (itemButtons.Contains(tempButton)) {
            if (tempItemData.Contains("questItem"))
            {
                inventory.MessageCantDrop(tempItemData["name"].ToString());
                return false;
            }

            if (checkAmmoInChest()) return true;
            ItemIcon chestButton = FirstEmptyChestButton;
            if (chestButton != null) {
                if (tempButton.myItemCode.Contains("key")) {
                    inventory.RemoveKey(tempButton.myItemCode);
                }
                ChangeItemButtons(tempButton, chestButton);
                SetTempButton(null, false);
                UpdateChestPositions();
            } else {
                inventory.ItemsMessage("space");
                return false;
            }
            return true;
        }

        //взять из сундука
        if(chestButtons.Contains(tempButton)) {
            bool isMoney = tempItemData["type"].ToString() == "money";
            if (checkMoneyInInventory(isMoney)) return true;

            if (checkAmmoInInventory()) return true;
            ItemIcon itemButton = FirstEmptyButton;
            if (itemButton != null) {
                if (tempButton.myItemCode.Contains("key")) {
                    inventory.AddKey(tempButton.myItemCode);
                }
                ChangeItemButtons(tempButton, itemButton);
                SetTempButton(null, false);
                UpdateChestPositions();
                
            } else {
                inventory.ItemsMessage("space");
                return false;
            }
        }

        return true;
    }

    //проверяем, есть ли в сундуке патроны, которые собираемся ложить
    private bool checkAmmoInChest()
    {
        if (chestButtons.Contains(tempButton)) return false;

        if (tempChest.ammoCount.Keys.Contains(tempButton.myItemCode)) {
            ItemIcon ammoButton = tempChest.ammoButtons[tempButton.myItemCode];
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

    private bool checkMoneyInInventory(bool isMoney)
    {
        if (isMoney) {
            inventory.money += tempChest.moneyCount;
            tempChest.moneyCount = 0;
            moneyCount.Text = inventory.money.ToString();
            tempButton.ClearItem();
            UpdateChestPositions();
        }
        return isMoney;
    }
}