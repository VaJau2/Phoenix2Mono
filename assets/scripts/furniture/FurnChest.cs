using Godot;
using Godot.Collections;

public class FurnChest: FurnBase {

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
            ammoCount.Add(ammoKey, startAmmoCount[ammoKey]);
        }
    }

    private bool mayOpen => (IsOpen == menu.isOpen);

    public override void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null)
    {
        if (!mayOpen) return;

        if (IsOpen) { 
            //если мебель и меню открыты
            //меню закроется и закроет мебель
            MenuManager.CloseMenu(menu);
        } else {
            base.ClickFurn();
            menu.ChangeMode(NewInventoryMode.Chest);
            ChestMode tempMode = menu.mode as ChestMode;
            tempMode.SetChest(this);
            MenuManager.TryToOpenMenu(menu);
            menu.Connect("MenuIsClosed", this, nameof(CloseFurn));
        }
    }

    public void CloseFurn()
    {
        base.ClickFurn();
        menu.Disconnect("MenuIsClosed", this, nameof(CloseFurn));
    }
}