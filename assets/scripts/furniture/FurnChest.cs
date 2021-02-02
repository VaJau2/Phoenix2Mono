using Godot;
using Godot.Collections;
using System;

public class FurnChest: FurnBase {

    InventoryMenu menu;
    [Export]
    public bool SpawnRandomItems;

    [Export]
    public string chestCode;
    [Export]
    public Array<string> itemCodes = new Array<string>();
    [Export]
    public Dictionary<string, int> ammoCount = new Dictionary<string, int>();
    public Dictionary<string, ItemIcon> ammoButtons = new Dictionary<string, ItemIcon>();
    public Dictionary<int, string> itemPositions = new Dictionary<int, string>();

    public override void _Ready()
    {
        base._Ready();
        menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");

        if (SpawnRandomItems) {
            RandomItems items = GetNode<RandomItems>("/root/Main/Scene/randomItems");
            RandomNumberGenerator rand = new RandomNumberGenerator();

            rand.Randomize();
            int itemsCount = rand.RandiRange(0, items.maxItemsCount);
            for (int i = 0; i < itemsCount; i++) {
                //берем рандомную вещь из списка
                int randItemNum = rand.RandiRange(0, items.itemCodes.Count - 1);
                string newItemCode = items.itemCodes[randItemNum];
                Dictionary itemData = ItemJSON.GetItemData(newItemCode);
                //если это патроны, ложим в список патронов
                if (itemData["type"].ToString() == "ammo") {
                    if (ammoCount.ContainsKey(newItemCode)) return;

                    int count = items.ammoCount[newItemCode];
                    ammoCount.Add(newItemCode, count);
                } else {
                    itemCodes.Add(newItemCode);
                }
            }
        }
    }

    private bool mayOpen => (IsOpen == menu.isOpen);

    public override void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null)
    {
        if (!mayOpen) return;

        if (IsOpen) { 
            //если мебель и меню открыты
            //меню закроется и закроет мебель
            menu.CloseMenu();
        } else {
            base.ClickFurn();
            menu.ChangeMode(NewInventoryMode.Chest);
            ChestMode tempMode = menu.mode as ChestMode;
            tempMode.SetChest(this);
            menu.OpenMenu();
            menu.Connect("MenuIsClosed", this, nameof(CloseFurn));
        }
    }

    public void CloseFurn()
    {
        base.ClickFurn();
        menu.Disconnect("MenuIsClosed", this, nameof(CloseFurn));
    }
}