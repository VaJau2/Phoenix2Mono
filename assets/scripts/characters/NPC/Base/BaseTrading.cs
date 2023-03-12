using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

//класс используется неписями-торговцами (пнями и роботоми)
//по сути, вытащенный за скобки общий функционал торговли
public static class BaseTrading
{
    static InventoryMenu menu;
    static bool isTrading = false;
    private static NPC tempNpc;
    
    public static void LoadTradingData(NPC npc)
    {
        if (!(npc is ITrader trader))
        {
            throw new ArgumentException();
        }

        tempNpc = npc;
        
        if (!Godot.Object.IsInstanceValid(menu))
        {
            menu = npc.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        }

        RandomItems items = npc.GetNode<RandomItems>("/root/Main/Scene/randomItems");
        if (trader.itemCodes.Count == 0 && trader.ammoCount.Count == 0)
        {
            items.LoadRandomItems(trader.itemCodes, trader.ammoCount);

            foreach (string itemCode in trader.startItemCodes) 
            {
                trader.itemCodes.Add(itemCode);
            }
            foreach (string ammoKey in trader.startAmmoCount.Keys) 
            {
                if (!trader.ammoCount.ContainsKey(ammoKey))
                {
                    trader.ammoCount.Add(ammoKey, trader.startAmmoCount[ammoKey]);
                }
            }
        }
    }

    public static void StartTrading()
    {
        if (!(tempNpc is ITrader trader))
        {
            throw new ArgumentException();
        }
        
        if (isTrading) 
        {
            MenuManager.CloseMenu(menu);
            isTrading = false;
        } 
        else 
        {
            menu.ChangeMode(NewInventoryMode.Trade);
            TradeMode tempMode = menu.mode as TradeMode;
            tempMode.SetTrader(trader);
            MenuManager.TryToOpenMenu(menu);
            menu.Connect("MenuIsClosed", tempNpc, nameof(ITrader.StopTrading));
            isTrading = true;
        }
    }

    public static void StopTrading()
    {
        isTrading = false;
        menu.Disconnect("MenuIsClosed", tempNpc, nameof(ITrader.StopTrading));
    }

    public static void LoadData(NPC npc, Dictionary data)
    {
        if (!(npc is ITrader trader))
        {
            throw new ArgumentException();
        }
        
        trader.startItemCodes.Clear();
        trader.startAmmoCount.Clear();
        
        Array newItemCodes = (Array) data["itemCodes"];
        Dictionary newAmmoCount = (Dictionary) data["ammoCount"];
        Dictionary newAmmoButtonNames = (Dictionary) data["ammoButtons"];
        Dictionary newItemPositions = (Dictionary) data["itemPositions"];

        trader.moneyCount = Convert.ToInt32(data["moneyCount"]);
        
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
        
        trader.ammoButtons.Clear();
        
        //текущая сцена во время загрузки данных еще не добавлена на уровень
        var scene = npc.GetOwner<Node>();
        foreach (string key in newAmmoButtonNames.Keys)
        {
            ItemIcon tempButton = (ItemIcon)Global.FindNodeInScene(scene, newAmmoButtonNames[key].ToString());
            trader.ammoButtons.Add(key, tempButton);
        }
        
        trader.itemPositions.Clear();
        foreach (string key in newItemPositions.Keys)
        {
            int intKey = Convert.ToInt32(key);
            trader.itemPositions.Add(intKey, newItemPositions[key].ToString());
        }
    }

    public static Dictionary GetSaveData(NPC npc)
    {
        if (!(npc is ITrader trader))
        {
            throw new ArgumentException();
        }
        
        //переводим массив патронных кнопок в массив имен кнопок
        var ammoButtonNames = new Dictionary<string, string>(); 
        foreach (string ammoType in trader.ammoButtons.Keys)
        {
            string buttonName = trader.ammoButtons[ammoType].Name;
            ammoButtonNames.Add(ammoType, buttonName);
        }
        
        Dictionary savedData = new Dictionary()
        {
            {"itemCodes", trader.itemCodes},
            {"ammoCount", trader.ammoCount},
            {"ammoButtons", ammoButtonNames},
            {"itemPositions", trader.itemPositions},
            {"moneyCount", trader.moneyCount},
        };

        return savedData;
    }
}