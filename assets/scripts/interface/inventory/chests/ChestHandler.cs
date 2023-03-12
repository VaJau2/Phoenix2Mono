using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class ChestHandler
{
    public bool IsBag { get; private set; }
    public string ChestCode { get; private set; }

    public int MoneyCount;

    public readonly Array<string> ItemCodes = new Array<string>();

    public readonly Dictionary<int, string> ItemPositions = new Dictionary<int, string>();
    
    //патроны считаются отдельно и не должны лежать в массиве вещей
    public readonly Dictionary<string, int> AmmoCount = new Dictionary<string, int>();

    public readonly Dictionary<string, ItemIcon> AmmoButtons = new Dictionary<string, ItemIcon>();
    
    public readonly InventoryMenu Menu;
    
    private readonly RandomNumberGenerator random = new RandomNumberGenerator();
    private readonly IChest chest;
    private Node chestNode;
    
    public ChestHandler(IChest chest)
    {
        this.chest = chest;
        chestNode = chest as Node;
        if (chestNode is null)
        {
            throw new ArgumentNullException(nameof(chestNode), "IChest is not node!");
        }
        
        Menu = chestNode.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
    }

    public ChestHandler SetCode(string code)
    {
        ChestCode = code;
        return this;
    }

    public ChestHandler SpawnRandomItems()
    {
        var items = chestNode.GetNode<RandomItems>("/root/Main/Scene/randomItems");
        items.LoadRandomItems(ItemCodes, AmmoCount);
        random.Randomize();
        if (random.Randf() < items.moneyChance) 
        {
            ItemCodes.Add(items.moneyNameCode);
            MoneyCount = random.RandiRange(0, items.maxMoneyCount);
        }
        return this;
    }

    public ChestHandler LoadStartItems(
        Array<string> startItemCodes,
        Dictionary<string, int> startAmmoCount)
    {
        foreach (var itemCode in startItemCodes) 
        {
            ItemCodes.Add(itemCode);
        }
        
        foreach (var ammoKey in startAmmoCount.Keys) 
        {
            if (!AmmoCount.ContainsKey(ammoKey)) 
            {
                AmmoCount.Add(ammoKey, startAmmoCount[ammoKey]);
            }
        }

        return this;
    }

    public ChestHandler SetIsBag(bool isBag)
    {
        IsBag = isBag;
        return this;
    }

    public void Open()
    {
        Menu.ChangeMode(NewInventoryMode.Chest);
        ChestMode tempMode = Menu.mode as ChestMode;
        tempMode?.SetChest(chest);
        MenuManager.TryToOpenMenu(Menu);
    }

    public Dictionary GetSaveData()
    {
        //переводим массив патронных кнопок в массив имен кнопок
        var ammoButtonNames = new Dictionary<string, string>(); 
        foreach (var ammoType in AmmoButtons.Keys)
        {
            var buttonName = AmmoButtons[ammoType].Name;
            ammoButtonNames.Add(ammoType, buttonName);
        }

        return new Dictionary
        {
            {"itemCodes", ItemCodes},
            {"ammoCount", AmmoCount},
            {"ammoButtons", ammoButtonNames},
            {"itemPositions", ItemPositions},
            {"moneyCount", MoneyCount},
        };
    }

    public void LoadData(Dictionary data)
    {
        if (!data.Contains("itemCodes")) return;
        
        Array newItemCodes = (Array) data["itemCodes"];
        Dictionary newAmmoCount = (Dictionary) data["ammoCount"];
        Dictionary newAmmoButtonNames = (Dictionary) data["ammoButtons"];
        Dictionary newItemPositions = (Dictionary) data["itemPositions"];

        MoneyCount = Convert.ToInt32(data["moneyCount"]);
        
        ItemCodes.Clear();
        foreach (string itemCode in newItemCodes)
        {
            ItemCodes.Add(itemCode);
        }

        AmmoCount.Clear();
        foreach (string key in newAmmoCount.Keys)
        {
            AmmoCount.Add(key, Convert.ToInt32(newAmmoCount[key]));
        }
        
        AmmoButtons.Clear();
        
        //текущая сцена во время загрузки данных еще не добавлена на уровень
        var scene = chestNode.GetOwner<Node>();
        foreach (string key in newAmmoButtonNames.Keys)
        {
            ItemIcon tempButton = (ItemIcon)Global.FindNodeInScene(scene, newAmmoButtonNames[key].ToString());
            AmmoButtons.Add(key, tempButton);
        }
        
        ItemPositions.Clear();
        foreach (string key in newItemPositions.Keys)
        {
            int intKey = Convert.ToInt32(key);
            ItemPositions.Add(intKey, newItemPositions[key].ToString());
        }
    }
    
    public void AddNewItem(string newItemCode)
    {
        //если сундук уже открывался, используется ItemPositions, а не itemCodes
        if (ItemPositions.Count > 0)
        {
            foreach (int i in ItemPositions.Keys)
            {
                if (!string.IsNullOrEmpty(ItemPositions[i])) continue;
                ItemPositions.Add(i, newItemCode);
                return;
            }
            ItemPositions.Add(ItemPositions.Count, newItemCode);
        }
        else
        {
            ItemCodes.Add(newItemCode);
        }
    }
}
