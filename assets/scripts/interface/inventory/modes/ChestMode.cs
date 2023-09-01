using System.Linq;
using Godot;
using Godot.Collections;

public class ChestMode: InventoryMode
{
    private Control chestBack;
    private Label chestLabel;
    private Button takeAll;

    private Array<ItemIcon> chestButtons = new Array<ItemIcon>();

    private IChest tempChest;

    public ChestMode (InventoryMenu menu)
    : base(menu)
    {
        chestBack  = back.GetNode<Control>("chestBack");
        chestLabel = chestBack.GetNode<Label>("Label");
        takeAll    = chestBack.GetNode<Button>("takeAll");
        foreach (object button in chestBack.GetNode<Control>("items").GetChildren()) 
        {
            chestButtons.Add(button as ItemIcon); 
        }
    }

    public void SetChest(IChest chest)
    {
        tempChest = chest;
        string chestName = InterfaceLang.GetPhrase("inGame", "chestNames", chest.ChestCode);
        chestLabel.Text = chestName;
        if(chest.ChestHandler.ItemPositions.Count > 0) 
        {
            LoadChestButtons(chest.ChestHandler.ItemPositions, chest.ChestHandler.AmmoCount);
        } 
        else 
        {
            LoadChestButtons(chest.ChestHandler.ItemCodes, chest.ChestHandler.AmmoCount);
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
        var point = menu.GetNode<InteractionPointManager>("/root/Main/Scene/canvas/pointManager");
        point.ShowSquareAgain();
        
        menu.EmitSignal(nameof(InventoryMenu.MenuIsClosed));
        chestBack.Visible = false;
        base.CloseMenu();
        menu.ChangeMode(NewInventoryMode.Usual);
    }

    public override void UpdateInput(InputEvent @event)
    {
        base.UpdateInput(@event);
        
        if (menu.isOpen)
        {
            if (tempButton != null) 
            {
                if (UpdateDragging(@event)) return;

                if (@event is InputEventKey && !chestButtons.Contains(tempButton)) 
                {
                    bindsHandler.BindHotkeys((ItemType)tempItemData["type"]);
                }
            }

            if (Input.IsActionJustPressed("ui_shift"))
            {
                _on_takeAll_pressed();
            }

            if (Input.IsActionJustPressed("use"))
            {
                MenuManager.CloseMenu(menu);
            }
        }
    }

    public override void RemoveItemFromButton(ItemIcon button)
    {
        base.RemoveItemFromButton(button);
        if (chestButtons.Contains(button))
        {
            UpdateChestPositions();
        }
    }

    public override async void _on_takeAll_pressed()
    {
        //чтоб щит не включался на ту же кнопку
        if (player is Player_Unicorn unicorn)
        {
            unicorn.shield.shieldCooldown = 0.5f;
        }
        
        foreach(ItemIcon tempIcon in chestButtons)
        {
            if (tempIcon.myItemCode != null) 
            {
                SetTempButton(tempIcon);
                if (!TakeTempItem()) return;
            }
        }
        await Global.Get().ToTimer(0.1f, null, true);
        if (tempChest == null) UpdateChestPositions();
        MenuManager.CloseMenu(menu);
    }

    private void ClearChestButtons()
    {
        foreach (ItemIcon tempIcon in chestButtons) 
        {
            tempIcon.ClearItem();
        }
    }

    //загружаем предметы, если сундук открывается впервые
    private void LoadChestButtons(Array<string> newItems, Dictionary<string, int> ammo)
    {
        ClearChestButtons();
        foreach (var item in newItems)
        {
            if (string.IsNullOrEmpty(item))
            {
                continue;
            }
            
            AddChestItem(item);
        }
        foreach (string ammoItem in ammo.Keys) 
        {
            ItemIcon newAmmoButton = AddChestItem(ammoItem);
            if (newAmmoButton == null) continue;
            
            newAmmoButton.SetCount(ammo[ammoItem]);
            tempChest.ChestHandler.AmmoButtons.Add(ammoItem, newAmmoButton);
        }
    }

    //загружаем предметы, если сундук уже открывался
    private void LoadChestButtons(Dictionary<int, string> ItemPositions, Dictionary<string, int> ammo)
    {
        ClearChestButtons();
        
        //в массиве ItemPositions также лежат патроны
        foreach(int buttonId in ItemPositions.Keys) 
        {
            string itemCode = ItemPositions[buttonId];
            ItemIcon tempChestButton = chestButtons[buttonId];
            CheckAddMoney(tempChestButton, itemCode);

            //грузим количество этих патронов
            if (ammo.ContainsKey(itemCode)) 
            {
                tempChestButton.SetCount(ammo[itemCode]);
            }
        }
    }

    private ItemIcon FirstEmptyChestButton => chestButtons.FirstOrDefault(button => button.myItemCode == null);

    private ItemIcon AddChestItem(string itemCode) 
    {
        ItemIcon emptyButton = FirstEmptyChestButton;
        if (emptyButton != null) 
        {
            CheckAddMoney(emptyButton, itemCode);
            
            int buttonId = chestButtons.IndexOf(emptyButton);
            if (!tempChest.ChestHandler.ItemPositions.ContainsKey(buttonId)) 
            {
                tempChest.ChestHandler.ItemPositions.Add(buttonId, itemCode);
            }
            
        } 
        else 
        {
            inventory.ItemsMessage("space");
        }
        
        return emptyButton;
    }

    private void CheckAddMoney(ItemIcon button, string itemCode)
    {
        button.SetItem(itemCode);

        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        if ((ItemType)itemData["type"] == ItemType.money)
        {
            button.SetCount(tempChest.ChestHandler.MoneyCount);
        }
    }

    //сохраняем в сундук позиции кнопок
    private void UpdateChestPositions() 
    {
        //очищаем все массивы сундука
        tempChest.ChestHandler.ItemCodes.Clear();
        tempChest.ChestHandler.ItemPositions.Clear();
        tempChest.ChestHandler.AmmoCount.Clear();
        tempChest.ChestHandler.AmmoButtons.Clear();
        bool isEmpty = true;

        //проходим по иконкам
        foreach (ItemIcon tempIcon in chestButtons) 
        {
            if (tempIcon.myItemCode != null)
            {
                isEmpty = false; //если есть вещи, то сундук не пустой

                //сохраняем позицию иконки
                int iconId = chestButtons.IndexOf(tempIcon);
                tempChest.ChestHandler.ItemPositions.Add(iconId, tempIcon.myItemCode);
                //сохраняем количество, если это патроны
                if (tempIcon.GetCount() != -1) 
                {
                    tempChest.ChestHandler.AmmoCount.Add(tempIcon.myItemCode, tempIcon.GetCount());
                    tempChest.ChestHandler.AmmoButtons.Add(tempIcon.myItemCode, tempIcon);
                }
            }
        }

        //если сундук - это сумка, и она опустошается
        //то она удаляется
        if (!isEmpty || !tempChest.ChestHandler.IsBag) return;
        if (tempChest is Node chestNode)
        {
            Global.AddDeletedObject(chestNode.Name);
            chestNode.QueueFree();
        }

        MenuManager.CloseMenu(menu);
    }

    private bool CheckDragIn(Array<ItemIcon> iconsArray, string ammoName)
    {
        foreach (ItemIcon otherButton in iconsArray) 
        {
            Control buttonControl = otherButton;
            if (tempButton != otherButton && CheckMouseInButton(buttonControl)) 
            {
                switch (ammoName) 
                {
                    case "chest":
                        if (CheckAmmoInChest()) return true;
                        break;
                    case "inventory":
                        if (CheckAmmoInInventory()) return true;
                        break;
                }

                ChangeItemButtons(tempButton, otherButton);
                SetTempButton(null, false);
                dragIcon.SetTexture(null);
                UpdateChestPositions();
                return true;
            }
        }
        return false;
    }

    protected override void CheckDragItem()
    {
        base.CheckDragItem();
        
        //перетащить из сундука
        if (CheckDragIn(itemButtons, "inventory")) return;

        if (tempItemData.Contains("questItem"))
        {
            inventory.MessageCantDrop(tempItemData["name"].ToString());
            return;
        }
        
        //перетащить в сундук
        CheckDragIn(chestButtons, "chest");
    }

    //грузим подсказки по управлению предметом
    protected override void LoadControlHint(bool isInventoryIcon)
    {
        var type = (ItemType)tempItemData["type"];
        ControlText[] controlTexts;

        switch (type)
        {
            case ItemType.weapon:
                controlTexts = isInventoryIcon 
                    ? new[] { ControlText.equip, ControlText.bind, ControlText.move, ControlText.put } 
                    : new [] { ControlText.equip, ControlText.move, ControlText.take };
                break;

            case ItemType.armor:
            case ItemType.artifact:
                controlTexts = isInventoryIcon 
                    ? new[] { ControlText.equip, ControlText.move, ControlText.put } 
                    : new [] { ControlText.equip, ControlText.move, ControlText.take };
                break;

            case ItemType.note:
                controlTexts = isInventoryIcon 
                    ? new[] { ControlText.read, ControlText.move, ControlText.put } 
                    : new [] { ControlText.read, ControlText.move, ControlText.take };
                break;

            case ItemType.food:
                controlTexts = isInventoryIcon 
                    ? new[] { ControlText.eat, ControlText.bind, ControlText.move, ControlText.put } 
                    : new [] { ControlText.eat, ControlText.bind, ControlText.move, ControlText.take };
                break;

            case ItemType.meds:
                controlTexts = isInventoryIcon 
                    ? new[] { ControlText.use, ControlText.bind, ControlText.move, ControlText.put } 
                    : new [] { ControlText.use, ControlText.bind, ControlText.move, ControlText.take };
                break;

            default:
                controlTexts = isInventoryIcon 
                    ? new[] { ControlText.move, ControlText.put }
                    : new [] { ControlText.move, ControlText.take };
                break;
        }
        
        controlHints.LoadHits(controlTexts);
    }

    public override void ChangeItemButtons(ItemIcon oldButton, ItemIcon newButton)
    {
        if (chestButtons.Contains(newButton) && !string.IsNullOrEmpty(oldButton.GetBindKey()))
        {
            bindsHandler.ClearBind(oldButton);
        }
        
        base.ChangeItemButtons(oldButton, newButton);

        if (chestButtons.Contains(oldButton) || chestButtons.Contains(newButton))
        {
            UpdateChestPositions();
        }
    }

    public override void MoveTempItem()
    {
        TakeTempItem();
    }

    private bool TakeTempItem()
    {
        //положить в сундук
        if (itemButtons.Contains(tempButton)) 
        {
            if (tempItemData.Contains("questItem"))
            {
                inventory.MessageCantDrop(tempItemData["name"].ToString());
                return false;
            }

            if (CheckAmmoInChest()) return true;
            ItemIcon chestButton = FirstEmptyChestButton;
            if (chestButton != null) 
            {
                if (tempButton.myItemCode.Contains("key")) 
                {
                    inventory.RemoveKey(tempButton.myItemCode);
                }
                ChangeItemButtons(tempButton, chestButton);
                SetTempButton(null, false);
            } 
            else 
            {
                inventory.ItemsMessage("space");
                return false;
            }
            return true;
        }

        //взять из сундука
        if(chestButtons.Contains(tempButton)) 
        {
            bool isMoney = (ItemType)tempItemData["type"] == ItemType.money;
            if (CheckMoneyInInventory(isMoney)) return true;
            if (CheckAmmoInInventory()) return true;
            
            ItemIcon itemButton = FirstEmptyButton;
            if (itemButton != null) 
            {
                if (tempButton.myItemCode.Contains("key")) 
                {
                    inventory.AddKey(tempButton.myItemCode);
                }
                ChangeItemButtons(tempButton, itemButton);
                SetTempButton(null, false);
            } 
            else
            {
                inventory.ItemsMessage("space");
                return false;
            }
        }

        return true;
    }

    //проверяем, есть ли в сундуке патроны, которые собираемся ложить
    private bool CheckAmmoInChest()
    {
        if (chestButtons.Contains(tempButton)) return false;

        if (tempChest.ChestHandler.AmmoCount.Keys.Contains(tempButton.myItemCode)) 
        {
            ItemIcon ammoButton = tempChest.ChestHandler.AmmoButtons[tempButton.myItemCode];
            int addCount = tempButton.GetCount();
            ammoButton.SetCount(ammoButton.GetCount() + addCount);
            tempButton.ClearItem();
            UpdateChestPositions();
            return true;
        }
        return false;
    }

    //проверяем, есть ли в инвентаре патроны, которые собираемся ложить
    private bool CheckAmmoInInventory()
    {
        if (itemButtons.Contains(tempButton)) return false;

        if (inventory.ammoButtons.Keys.Contains(tempButton.myItemCode)) 
        {
            ItemIcon ammoButton = inventory.ammoButtons[tempButton.myItemCode];
            int addCount = tempButton.GetCount();
            ammoButton.SetCount(ammoButton.GetCount() + addCount);
            tempButton.ClearItem();
            UpdateChestPositions();

            if (player.Weapons.tempAmmoButton == ammoButton) 
            {
                player.Weapons.UpdateAmmoCount();
            }

            return true;
        }
        return false;
    }

    private bool CheckMoneyInInventory(bool isMoney)
    {
        if (!isMoney) return false;
        inventory.money += tempChest.ChestHandler.MoneyCount;
        tempChest.ChestHandler.MoneyCount = 0;
        moneyCount.Text = inventory.money.ToString();
        tempButton.ClearItem();
        UpdateChestPositions();
        return true;
    }
}