using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class FurnChest: FurnBase, ISavable {

    InventoryMenu menu;
    [Export]
    public bool isBag;
    [Export]
    public bool SpawnRandomItems;

    [Export]
    public string chestCode;
    [Export]
    public Array<string> startItemCodes = new Array<string>();
    [Export]
    public Dictionary<string, int> startAmmoCount = new Dictionary<string, int>();

    //каким-то образом export-массивы сохраняются между сессиями
    //и если уровень перезагружается, игра получает их уже заполненными
    public Array<string> itemCodes = new Array<string>();
    //патроны считаются отдельно и не должны лежать в массиве вещей
    public Dictionary<string, int> ammoCount = new Dictionary<string, int>();
    public Dictionary<string, ItemIcon> ammoButtons = new Dictionary<string, ItemIcon>();
    public Dictionary<int, string> itemPositions = new Dictionary<int, string>();
    public int moneyCount = 0;
    RandomNumberGenerator random = new RandomNumberGenerator();

    public override void _Ready()
    {
        base._Ready();
        menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");

        if (SpawnRandomItems) {
            RandomItems items = GetNode<RandomItems>("/root/Main/Scene/randomItems");
            items.LoadRandomItems(itemCodes, ammoCount);
            
            random.Randomize();
            if (random.Randf() < items.moneyChance) {
                itemCodes.Add(items.moneyNameCode);
                moneyCount = random.RandiRange(0, items.maxMoneyCount);
            }
        }

        foreach(string itemCode in startItemCodes) {
            itemCodes.Add(itemCode);
        }
        foreach(string ammoKey in startAmmoCount.Keys) {
            if (!ammoCount.ContainsKey(ammoKey)) 
            {
                ammoCount.Add(ammoKey, startAmmoCount[ammoKey]);
            }
        }
    }

    private bool mayOpen => (IsOpen == menu.isOpen);

    public override void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null)
    {
        if (!mayOpen) return;

        if (IsOpen) return;
        base.ClickFurn();
        menu.ChangeMode(NewInventoryMode.Chest);
        ChestMode tempMode = menu.mode as ChestMode;
        tempMode?.SetChest(this);
        MenuManager.TryToOpenMenu(menu);
        menu.Connect("MenuIsClosed", this, nameof(CloseFurn));
    }

    public void CloseFurn()
    {
        base.ClickFurn();
        menu.Disconnect("MenuIsClosed", this, nameof(CloseFurn));
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
        
        return new Dictionary()
        {
            {"parent", GetParent().Name},
            {"fileName", Filename},
            
            {"pos_x", GlobalTransform.origin.x},
            {"pos_y", GlobalTransform.origin.y},
            {"pos_z", GlobalTransform.origin.z},
            {"rot_x", GlobalTransform.basis.GetEuler().x},
            {"rot_y", GlobalTransform.basis.GetEuler().y},
            {"rot_z", GlobalTransform.basis.GetEuler().z},
            
            {"itemCodes", itemCodes},
            {"ammoCount", ammoCount},
            {"ammoButtons", ammoButtonNames},
            {"itemPositions", itemPositions},
            {"moneyCount", moneyCount},
        };
    }

    public void LoadData(Dictionary data)
    {
        Vector3 newPos = new Vector3(Convert.ToSingle(data["pos_x"]), Convert.ToSingle(data["pos_y"]), Convert.ToSingle(data["pos_z"]));
        Vector3 newRot = new Vector3(Convert.ToSingle(data["rot_x"]), Convert.ToSingle(data["rot_y"]), Convert.ToSingle(data["rot_z"]));

        Basis newBasis = new Basis(newRot);
        Transform newTransform = new Transform(newBasis, newPos);
        GlobalTransform = newTransform;
        
        Array newItemCodes = (Array) data["itemCodes"];
        Dictionary newAmmoCount = (Dictionary) data["ammoCount"];
        Dictionary newAmmoButtonNames = (Dictionary) data["ammoButtons"];
        Dictionary newItemPositions = (Dictionary) data["itemPositions"];

        moneyCount = Convert.ToInt32(data["moneyCount"]);
        
        itemCodes.Clear();
        foreach (string itemCode in newItemCodes)
        {
            itemCodes.Add(itemCode);
        }

        ammoCount.Clear();
        foreach (string key in newAmmoCount.Keys)
        {
            ammoCount.Add(key, Convert.ToInt32(newAmmoCount[key]));
        }
        
        ammoButtons.Clear();
        //пока сундук не добавлен на сцену, его GetNode не работает
        var scene = Global.Get().player.GetNode("/root/Main/Scene");
        foreach (string key in newAmmoButtonNames.Keys)
        {
            ItemIcon tempButton = (ItemIcon)Global.FindNodeInScene(scene, newAmmoButtonNames[key].ToString());
            ammoButtons.Add(key, tempButton);
        }
        
        itemPositions.Clear();
        foreach (string key in newItemPositions.Keys)
        {
            int intKey = Convert.ToInt32(key);
            itemPositions.Add(intKey, newItemPositions[key].ToString());
        }
    }
}