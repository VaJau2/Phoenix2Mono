using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

//класс используется неписями-торговцами (пнями и роботоми)
//по сути, вытащенный за скобки общий функционал торговли
public class NpcTrading: Node, ISavable, ITrader
{
    [Export]
    public int moneyCount {get; set;}   
    [Export]
    public string traderCode {get; set;}
    [Export]
    public Array<string> startItemCodes {get; set;} = new();
    [Export]
    public Dictionary<string, int> startAmmoCount {get; set;} = new();

    public Array<string> itemCodes {get; set;} = new();
    public Dictionary<string, int> ammoCount {get; set;} = new();
    public Dictionary<string, ItemIcon> ammoButtons {get; set;} = new();
    public Dictionary<int, string> itemPositions {get; set;} = new();
    
    private InventoryMenu menu;
    private bool isTrading;
    private NPC trader;

    public override void _Ready()
    {
        menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        trader = GetParent<NPC>();
        LoadTradingData();
    }
    
    public void StartTrading()
    {
        if (isTrading) 
        {
            MenuManager.CloseMenu(menu);
            isTrading = false;
        } 
        else 
        {
            menu.ChangeMode(NewInventoryMode.Trade);
            if (menu.mode is not TradeMode tempMode)
            {
                return;
            }
            
            tempMode.SetTrader(this);
            MenuManager.TryToOpenMenu(menu);
            menu.Connect("MenuIsClosed", this, nameof(StopTrading));
            isTrading = true;
        }
    }

    public void StopTrading()
    {
        isTrading = false;
        menu.Disconnect("MenuIsClosed", this, nameof(StopTrading));
    }

    private void LoadTradingData()
    {
        var items = GetNode<RandomItems>("/root/Main/Scene/randomItems");
        if (trader.itemCodes.Count != 0 || trader.ammoCount.Count != 0)
        {
            return;
        }
        
        items.LoadRandomItems(trader.itemCodes, trader.ammoCount);

        foreach (var itemCode in startItemCodes) 
        {
            trader.itemCodes.Add(itemCode);
        }
        foreach (var ammoKey in startAmmoCount.Keys) 
        {
            if (!trader.ammoCount.ContainsKey(ammoKey))
            {
                trader.ammoCount.Add(ammoKey, startAmmoCount[ammoKey]);
            }
        }
    }

    public void LoadData(Dictionary data)
    {
        Array newItemCodes = (Array) data["itemCodes"];
        Dictionary newAmmoCount = (Dictionary) data["ammoCount"];
        Dictionary newAmmoButtonNames = (Dictionary) data["ammoButtons"];
        Dictionary newItemPositions = (Dictionary) data["itemPositions"];

        moneyCount = Convert.ToInt32(data["moneyCount"]);
        
        trader.itemCodes.Clear();
        foreach (string itemCode in newItemCodes)
        {
            trader.itemCodes.Add(itemCode);
        }

        trader.ammoCount.Clear();
        foreach (string key in newAmmoCount.Keys)
        {
            trader.ammoCount.Add(key, Convert.ToInt32(newAmmoCount[key]));
        }
        
        ammoButtons.Clear();
        
        //текущая сцена во время загрузки данных еще не добавлена на уровень
        var scene = GetOwner<Node>();
        foreach (string key in newAmmoButtonNames.Keys)
        {
            ItemIcon tempButton = (ItemIcon)Global.FindNodeInScene(scene, newAmmoButtonNames[key].ToString());
            ammoButtons.Add(key, tempButton);
        }
        

        foreach (string key in newItemPositions.Keys)
        {
            int intKey = Convert.ToInt32(key);
            itemPositions.Add(intKey, newItemPositions[key].ToString());
        }
    }

    public Dictionary GetSaveData()
    {
        //переводим массив патронных кнопок в массив имен кнопок
        var ammoButtonNames = new Dictionary<string, string>(); 
        foreach (string ammoType in ammoButtons.Keys)
        {
            string buttonName = ammoButtons[ammoType].Name;
            ammoButtonNames.Add(ammoType, buttonName);
        }
        
        Dictionary savedData = new Dictionary()
        {
            {"itemCodes", trader.itemCodes},
            {"ammoCount", trader.ammoCount},
            {"ammoButtons", ammoButtonNames},
            {"itemPositions", itemPositions},
            {"moneyCount", moneyCount},
        };

        return savedData;
    }
}
