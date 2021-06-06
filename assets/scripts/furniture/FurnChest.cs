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

        Dictionary savedData = new Dictionary()
        {
            {"itemCodes", itemCodes},
            {"ammoCount", ammoCount},
            {"ammoButtons", ammoButtonNames},
            {"itemPositions", itemPositions},
            {"moneyCount", moneyCount},
        };

        if (Name.BeginsWith("Created_"))
        {
            savedData.Add("parent", GetParent().Name);
            savedData.Add("fileName", Filename);
            
            savedData.Add("pos_x", Transform.origin.x);
            savedData.Add("pos_y", Transform.origin.y);
            savedData.Add("pos_z", Transform.origin.z);
            savedData.Add("rot_x", Transform.basis.GetEuler().x);
            savedData.Add("rot_y", Transform.basis.GetEuler().y);
            savedData.Add("rot_z", Transform.basis.GetEuler().z);
        }

        return savedData;
    }

    public void LoadData(Dictionary data)
    {
        SpawnRandomItems = false;
        startItemCodes.Clear();
        startAmmoCount.Clear();
        
        if (Name.BeginsWith("Created_"))
        {
            Vector3 newPos = new Vector3(Convert.ToSingle(data["pos_x"]), Convert.ToSingle(data["pos_y"]),
                Convert.ToSingle(data["pos_z"]));
            Vector3 newRot = new Vector3(Convert.ToSingle(data["rot_x"]), Convert.ToSingle(data["rot_y"]),
                Convert.ToSingle(data["rot_z"]));

            Basis newBasis = new Basis(newRot);
            Transform newTransform = new Transform(newBasis, newPos);
            Transform = newTransform;
        }

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
        
        //текущая сцена во время загрузки данных еще не добавлена на уровень
        var scene = GetOwner<Node>();
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