using Godot;
using Godot.Collections;

public partial class TradeTerminal: StaticBody3D, ITrader 
{
    [Export]
    public int moneyCount {get; set;}   
    [Export]
    public string traderCode {get; set;}
    [Export]
    public Array<string> startItemCodes {get; set;} = [];
    [Export]
    public Dictionary<string, int> startAmmoCount {get; set;} = new();

    public Array<string> itemCodes {get; set;} = [];
    public Dictionary<string, int> ammoCount {get; set;} = new();
    public Dictionary<string, ItemIcon> ammoButtons {get; set;} = new();
    public Dictionary<int, string> itemPositions {get; set;} = new();
    
    InventoryMenu menu;
    bool isTrading = false;
    
    public override void _Ready()
    {
        menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");

        RandomItems items = GetNode<RandomItems>("/root/Main/Scene/randomItems");
        items.LoadRandomItems(itemCodes, ammoCount);

        foreach (string itemCode in startItemCodes)
        {
            itemCodes.Add(itemCode);
        }

        foreach (string ammoKey in startAmmoCount.Keys)
        {
            if (!ammoCount.ContainsKey(ammoKey))
            {
                ammoCount.Add(ammoKey, startAmmoCount[ammoKey]);
            }
        }
    }

    private bool mayOpen => (isTrading == menu.isOpen);

    public void StartTrading()
    {
        if (!mayOpen) return;

        if (isTrading) {
            MenuManager.CloseMenu(menu);
            isTrading = false;
        } else {
            menu.ChangeMode(NewInventoryMode.Trade);
            TradeMode tempMode = menu.mode as TradeMode;
            tempMode?.SetTrader(this);
            MenuManager.TryToOpenMenu(menu);
            menu.MenuIsClosed += StopTrading;
            isTrading = true;
        }
    }

    public void StopTrading()
    {
        isTrading = false;
        menu.MenuIsClosed -= StopTrading;
    }
}