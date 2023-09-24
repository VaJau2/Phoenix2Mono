using System.Globalization;
using System.Linq;
using Godot;
using Godot.Collections;

public class TradeMode: InventoryMode 
{
    const float PRICE_DIFF = 0.8f;

    private Control tradeBack;
    private Label tradeLabel;
    private Label traderMoneyCount;

    private Array<ItemIcon> tradeButtons = new Array<ItemIcon>();
    private RandomItems randomItems;
    private Label askLabel;
    private Button askYes, askNo;
    private Control sliderBack;
    private Label sliderLabel;
    private Label sliderCount;
    private Slider slider;

    private ITrader tempTrader;
    private string tempItemName;
    private int tempItemPrice;
    private int tempCount = 1;
    private int tempCountMax = 1;
    private string tempAction;

    public TradeMode (InventoryMenu menu)
    : base(menu)
    {
        var back = menu.GetNode<Control>("helper/back");
        randomItems = menu.GetNode<RandomItems>("/root/Main/Scene/randomItems");
        labels.Add("moneyTrade", back.GetNode<Label>("tradeBack/moneyLabel"));

        tradeBack = back.GetNode<Control>("tradeBack");
        tradeLabel = tradeBack.GetNode<Label>("Label");
        traderMoneyCount = tradeBack.GetNode<Label>("moneyCount");
        foreach (object button in tradeBack.GetNode<Control>("items").GetChildren()) 
        {
            tradeButtons.Add(button as ItemIcon); 
        }

        askLabel = modalAsk.GetNode<Label>("askLabel");
        askYes = modalAsk.GetNode<Button>("yes");
        askNo  = modalAsk.GetNode<Button>("no");
        sliderBack  = modalAsk.GetNode<Control>("sliderBack");
        slider      = sliderBack.GetNode<Slider>("slider");
        sliderLabel = sliderBack.GetNode<Label>("sliderLabel");
        sliderCount = sliderBack.GetNode<Label>("sliderCount");
    }

    public void SetTrader(ITrader trader)
    {
        tempTrader = trader;
        string traderName = InterfaceLang.GetPhrase("inGame", "tradeNames", trader.traderCode);
        tradeLabel.Text = traderName;
        if(trader.itemPositions.Count > 0) 
        {
            LoadTradeButtons(trader.itemPositions, trader.ammoCount);
        } 
        else 
        {
            LoadTradeButtons(trader.itemCodes, trader.ammoCount);
        }
    }

    public override void OpenMenu()
    {
        base.OpenMenu();
        tradeBack.Visible = true;
        traderMoneyCount.Text = tempTrader.moneyCount.ToString();
    }

    public override void CloseMenu()
    {
        menu.EmitSignal(nameof(InventoryMenu.MenuIsClosed));
        _on_modal_no_pressed();
        tradeBack.Visible = false;
        base.CloseMenu();
        menu.ChangeMode(NewInventoryMode.Usual);
    }

    public override void UpdateInput(InputEvent @event)
    {
        if (menu.isOpen && tempButton != null) 
        {
            if (modalAsk.Visible) 
            {
                if (Input.IsActionJustPressed("jump")) 
                {
                    _on_modal_yes_pressed();
                }
                if (Input.IsActionJustPressed("ui_shift")) 
                {
                    _on_modal_no_pressed();
                }
            } 
            else 
            {
                if (tempButton.isInventoryIcon)
                {
                    if (UpdateDragging(@event)) return;
                }
                
                if (Input.IsActionJustReleased("ui_click")) 
                {
                    if (IsMouseOutsideDeadZone())
                    {
                        if (CheckSellItem()) return;
                        CheckBuyItem();
                    }
                }
                
                if (@event is InputEventKey && !tradeButtons.Contains(tempButton)) 
                {
                    bindsHandler.BindHotkeys((ItemType)tempItemData["type"]);
                }
            }
        }
        
        base.UpdateInput(@event);
    }

    public override void MoveTempItem()
    {
        if (CheckSellItem()) return;
        CheckBuyItem();
    }

    public override void _on_modal_no_pressed()
    {
        CloseModalAsk();
    }

    public override void _on_modal_yes_pressed()
    {
        int itemPrice = tempItemPrice * tempCount;
        
        if (tempAction == "sell")
        {
            if (tempTrader.moneyCount < itemPrice)
            {
                inventory.ItemsMessage("money");
                return;
            }
            
            //продажа товара
            tempTrader.moneyCount -= itemPrice;
            inventory.money += itemPrice;

            if (!checkAmmoInTrader())
            {
                ItemIcon traderButton = FirstEmptyTradeButton;
                if (traderButton != null)
                {
                    if (tempCount == tempCountMax)
                    {
                        ChangeItemButtons(tempButton, traderButton);
                    }
                    else
                    {
                        traderButton.SetItem(tempButton.myItemCode);
                        traderButton.SetCount(tempCount);
                        tempButton.SetCount(tempButton.GetCount() - tempCount, true);
                    }

                    UpdateTraderPositions();
                }
                else
                {
                    //если у торговца нет места, предмет пропадает
                    RemoveItemFromButton(tempButton);
                    UpdateMoneyCount();
                }
            }
        } 
        else 
        {
            if (inventory.money < itemPrice)
            {
                inventory.ItemsMessage("money");
                return;
            }
            
            //покупка товара
            tempTrader.moneyCount += tempItemPrice * tempCount;
            inventory.money -= tempItemPrice * tempCount;
            
            if (!checkAmmoInInventory()) 
            {
                ItemIcon itemButton = FirstEmptyButton;
                if (tempCount == tempCountMax) 
                {
                    ChangeItemButtons(tempButton, itemButton);
                } 
                else 
                {
                    itemButton.SetItem(tempButton.myItemCode);
                    itemButton.SetCount(tempCount);
                    tempButton.SetCount(tempButton.GetCount() - tempCount, true);
                }
                
                UpdateTraderPositions();
            } 
        }

        CloseModalAsk();
    }

    public override void _on_count_value_changed(float newCount)
    {
        tempCount = (int)newCount;
        string askPhrase = GetAskPhrase();
        askLabel.Text = askPhrase;
        sliderCount.Text = newCount.ToString(CultureInfo.InvariantCulture);
    }

    private void CloseModalAsk()
    {
        askYes.Disabled = true;
        askNo.Disabled = true;
        modalAsk.Visible = false;
        tempButton?._on_itemIcon_mouse_exited();
    }

    private string GetMoneyName()
    {
        string moneyName = randomItems.moneyNameCode;
        string money1 = InterfaceLang.GetPhrase("inventory", "moneyNames", moneyName + "1");
        string money2 = InterfaceLang.GetPhrase("inventory", "moneyNames", moneyName + "2");
        string money5 = InterfaceLang.GetPhrase("inventory", "moneyNames", moneyName + "5");
        return Global.GetCountWord(tempItemPrice * tempCount, money1, money2, money5);
    }
    
    private string GetAskPhrase()
    {
        string askPhraseName = tempAction == "sell" ? "askSell" : "askBuy";
        string askPhrase = InterfaceLang.GetPhrase("inventory", "modalAsk", askPhraseName);
        string itemPrice = (tempItemPrice * tempCount).ToString();

        askPhrase = askPhrase.Replace("#item#", tempItemName);
        askPhrase = askPhrase.Replace("#price#", itemPrice);
        askPhrase = askPhrase.Replace("#money#", GetMoneyName());
        return askPhrase;
    }
    
    private async void OpenModalAsk(string action)
    {
        //грузим фразу: купить "предмет" за 42 бита?
        tempAction = action;

        //грузим кнопки для кнопок
        string askYesText = InterfaceLang.GetPhrase("inventory", "modalAsk", "yes");
        string askNoText = InterfaceLang.GetPhrase("inventory", "modalAsk", "no");

        askLabel.Text    = GetAskPhrase();
        askYes.Text      = askYesText.Replace("#button#", Global.GetKeyName("jump"));
        askNo.Text       = askNoText.Replace("#button#", Global.GetKeyName("ui_shift"));
        sliderLabel.Text = InterfaceLang.GetPhrase("inventory", "modalAsk", "countLabel");
        sliderCount.Text = "1";
        tempCount = 1;
        slider.Value = tempCount;
        slider.MaxValue = tempCountMax;
        sliderBack.Visible = tempCountMax > tempCount;
        modalAsk.Visible = true;

        await player.ToSignal(player.GetTree(), "idle_frame");
        askYes.Disabled = false;
        askNo.Disabled = false;
    }

    private void ClearTraderButtons()
    {
        foreach(ItemIcon tempIcon in tradeButtons)
        {
            tempIcon.ClearItem();
        }
    }

    //загружаем предметы, если торговля начинается впервые
    private void LoadTradeButtons(Array<string> newItems, Dictionary<string, int> ammo)
    {
        ClearTraderButtons();
        foreach (var item in newItems)
        {
            AddTradeItem(item);
        }
        foreach(string ammoItem in ammo.Keys) 
        {
            ItemIcon newAmmoButton = AddTradeItem(ammoItem);
            newAmmoButton.SetCount(ammo[ammoItem]);
            tempTrader.ammoButtons.Add(ammoItem, newAmmoButton);
        }
    }

    //загружаем предметы, если уже торговались
    private void LoadTradeButtons(Dictionary<int, string> itemPositions, Dictionary<string, int> ammo)
    {
        ClearTraderButtons();
        
        //в массиве itemPositions также лежат патроны
        foreach (int buttonId in itemPositions.Keys) 
        {
            string itemCode = itemPositions[buttonId];
            ItemIcon tempTradeButton = tradeButtons[buttonId];
            tempTradeButton.SetItem(itemCode);

            //грузим количество этих патронов
            if(ammo.ContainsKey(itemCode)) 
            {
                tempTradeButton.SetCount(ammo[itemCode]);
            }
        }
    }

    private ItemIcon FirstEmptyTradeButton 
    {
        get { return tradeButtons.FirstOrDefault(button => button.myItemCode == null); }
    }

    private ItemIcon AddTradeItem(string itemCode) 
    {
        ItemIcon emptyButton = FirstEmptyTradeButton;
        if (emptyButton != null) 
        {
            emptyButton.SetItem(itemCode);
            
            int buttonId = tradeButtons.IndexOf(emptyButton);
            tempTrader.itemPositions.Add(buttonId, itemCode);
        } 
        else 
        {
            inventory.ItemsMessage("space");
        }
        
        return emptyButton;
    }

    //сохраняем у торговца позиции кнопок после торговли
    private void UpdateTraderPositions() 
    {
        //очищаем все массивы торговца
        tempTrader.itemCodes.Clear();
        tempTrader.itemPositions.Clear();
        tempTrader.ammoCount.Clear();
        tempTrader.ammoButtons.Clear();

        //проходим по иконкам
        foreach(ItemIcon tempIcon in tradeButtons) 
        {
            if (tempIcon.myItemCode != null)
            {
                //сохраняем позицию иконки
                int iconId = tradeButtons.IndexOf(tempIcon);
                tempTrader.itemPositions.Add(iconId, tempIcon.myItemCode);
                
                //сохраняем количество, если это патроны
                if(tempIcon.GetCount() != -1) 
                {
                    tempTrader.ammoCount.Add(tempIcon.myItemCode, tempIcon.GetCount());
                    tempTrader.ammoButtons.Add(tempIcon.myItemCode, tempIcon);
                }
            }
        }

        UpdateMoneyCount();
    }

    private void UpdateMoneyCount()
    {
        moneyCount.Text = inventory.money.ToString();
        traderMoneyCount.Text = tempTrader.moneyCount.ToString();
    }

    //грузим подсказки по управлению предметом
    protected override void LoadControlHint(bool isInventoryIcon)
    {
        var type = (ItemType)tempItemData["type"];
        ControlText[] controlTexts;

        if (isInventoryIcon)
        {
            switch (type)
            {
                case ItemType.weapon:
                    controlTexts = new[] { ControlText.equip, ControlText.bind, ControlText.move, ControlText.sell };
                    break;

                case ItemType.armor:
                case ItemType.artifact:
                    controlTexts = new[] { ControlText.equip, ControlText.move, ControlText.sell };
                    break;

                case ItemType.note:
                    controlTexts = new[] { ControlText.read, ControlText.move, ControlText.sell };
                    break;

                case ItemType.food:
                    controlTexts = new[] { ControlText.eat, ControlText.bind, ControlText.move, ControlText.sell };
                    break;

                case ItemType.meds:
                    controlTexts = new[] { ControlText.use, ControlText.bind, ControlText.move, ControlText.sell };
                    break;

                default:
                    controlTexts = new[] { ControlText.move, ControlText.sell };
                    break;
            }
        }
        else controlTexts = new[] { ControlText.buy };

        controlHints.LoadHits(controlTexts);
    }

    public override void ChangeItemButtons(ItemIcon oldButton, ItemIcon newButton)
    {
        if (tradeButtons.Contains(newButton) && !string.IsNullOrEmpty(oldButton.GetBindKey()))
        {
            bindsHandler.ClearBind(oldButton);
        }

        base.ChangeItemButtons(oldButton, newButton);
    }

    private int GetItemPrice(Dictionary tempItemProps, bool buyPrice)
    {
        int tempPrice = int.Parse(tempItemProps["price"].ToString());
        float priceDelta = player.PriceDelta;
        
        if (buyPrice) 
        {
            //стоимость покупки
            tempPrice = (int)(tempPrice / (PRICE_DIFF * priceDelta));
        } 
        else 
        {
            //стоимость продажи
            tempPrice = (int)(tempPrice * (PRICE_DIFF * priceDelta));
            if (tempPrice == 0)
            {
                tempPrice = 1;
            }
        }

        //уровень инфляции из настроек сложности
        var inflationPrice = (float)tempPrice;
        inflationPrice *= Global.Get().Settings.inflation;

        return (int)inflationPrice;
    }

    protected override string GetItemPropsString(Dictionary itemProps)
    {
        string result = "";
        Dictionary itemPropNames = InterfaceLang.GetPhrasesSection("inventory", "itemProps");
        foreach(string prop in itemProps.Keys) 
        {
            if (itemPropNames.Contains(prop)) 
            {
                if (tempButton.myItemCode is "power-armor" && (prop is "checkHasItem" or "onlyForEarthponies")) 
                    continue;
                
                string propName = itemPropNames[prop].ToString();
                string propValue;
                
                //выводим стоимость покупки/продажи предмета
                if (prop == "price") 
                {
                    tempItemName = itemProps["name"].ToString();
                    
                    bool isBuyPrice = !itemButtons.Contains(tempButton);
                    tempItemPrice = GetItemPrice(itemProps, isBuyPrice);
                    
                    propValue = tempItemPrice.ToString(); 
                } 
                else 
                {
                    propValue = itemProps[prop].ToString();
                }

                propValue = ConvertPropValue(prop, propValue);

                result += "> " + propName + propValue + "\n";
            }
        }
        
        tempCountMax = (ItemType)itemProps["type"] == ItemType.ammo ? tempButton.GetCount() : 1;
        return result;
    } 

    private bool CheckSellItem()
    {
        if (itemButtons.Contains(tempButton)) 
        {
            //если предмет квестовый, его нельзя продать
            if (tempItemData.Contains("questItem"))
            {
                inventory.MessageCantSell(tempItemData["name"].ToString());
                return true;
            }

            //если у торговца хватает денег
            if (tempTrader.moneyCount > tempItemPrice)
            {
                OpenModalAsk("sell");
                return true;
            } 
            
            inventory.ItemsMessage("money");
            return true;
        }

        return false;
    }

    private void CheckBuyItem()
    {
        if (tradeButtons.Contains(tempButton)) 
        {
            ItemIcon itemButton = FirstEmptyButton;
            if (itemButton != null || buyAmmo) 
            {
                //если в инвентаре хватает денег
                if (inventory.money > tempItemPrice) 
                {
                    OpenModalAsk("buy");
                    return;

                } 
                inventory.ItemsMessage("money");
            } 
            else 
            {
                inventory.ItemsMessage("space");
            }
        }

    }

    private bool buyAmmo => inventory.ammoButtons.Keys.Contains(tempButton.myItemCode);

    //проверяем, есть ли у торговца патроны, которые собираемся продать
    private bool checkAmmoInTrader()
    {
        if (tradeButtons.Contains(tempButton)) return false;

        if (tempTrader.ammoButtons.Keys.Contains(tempButton.myItemCode)) 
        {
            ItemIcon ammoButton = tempTrader.ammoButtons[tempButton.myItemCode];
            int addCount = tempCount;
            ammoButton.SetCount(ammoButton.GetCount() + addCount);
            tempButton.SetCount(tempButton.GetCount() - tempCount, true);
            UpdateTraderPositions();
            return true;
        }
        return false;
    }


    //проверяем, есть ли в инвентаре патроны, которые собираемся купить
    private bool checkAmmoInInventory()
    {
        if (itemButtons.Contains(tempButton)) return false;

        if (buyAmmo) 
        {
            ItemIcon ammoButton = inventory.ammoButtons[tempButton.myItemCode];
            int addCount = tempCount;
            ammoButton.SetCount(ammoButton.GetCount() + addCount);
            tempButton.SetCount(tempButton.GetCount() - tempCount, true);
            
            UpdateTraderPositions();

            if (player.Weapons.tempAmmoButton == ammoButton) 
            {
                player.Weapons.UpdateAmmoCount();
            }

            return true;
        }
        return false;
    }
}
