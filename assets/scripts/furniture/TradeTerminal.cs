using Godot;
using Godot.Collections;

public class TradeTerminal: StaticBody, ITrader 
{
    [Export]
    public int moneyCount {get; set;}   
    [Export]
    public string traderCode {get; set;}
    [Export]
    public Array<string> startItemCodes {get; set;} = new Array<string>();
    [Export]
    public Dictionary<string, int> startAmmoCount {get; set;} = new Dictionary<string, int>();

    public Array<string> itemCodes {get; set;} = new Array<string>();
    public Dictionary<string, int> ammoCount {get; set;} = new Dictionary<string, int>();
    public Dictionary<string, ItemIcon> ammoButtons {get; set;} = new Dictionary<string, ItemIcon>();
    public Dictionary<int, string> itemPositions {get; set;} = new Dictionary<int, string>();
    InventoryMenu menu;
    bool isTrading = false;
    public override void _Ready()
    {
        menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");

        RandomItems items = GetNode<RandomItems>("/root/Main/Scene/randomItems");
        items.LoadRandomItems(itemCodes, ammoCount);

        foreach(string itemCode in startItemCodes) {
            itemCodes.Add(itemCode);
        }
        foreach(string ammoKey in startAmmoCount.Keys) {
            ammoCount.Add(ammoKey, startAmmoCount[ammoKey]);
        }
    }

    private bool mayOpen => (isTrading == menu.isOpen);

    public void StartTrading()
    {
        if (!mayOpen) return;

        if (isTrading) {
            menu.CloseMenu();
            isTrading = false;
        } else {
            menu.ChangeMode(NewInventoryMode.Trade);
            TradeMode tempMode = menu.mode as TradeMode;
            tempMode.SetTrader(this);
            menu.OpenMenu();
            menu.Connect("MenuIsClosed", this, nameof(StopTrading));
            isTrading = true;
        }
    }

    private void StopTrading()
    {
        isTrading = false;
        menu.Disconnect("MenuIsClosed", this, nameof(StopTrading));
    }
}