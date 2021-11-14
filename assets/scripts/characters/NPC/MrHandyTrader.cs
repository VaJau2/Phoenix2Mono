using Godot;
using Godot.Collections;

public class MrHandyTrader: MrHandy, ITrader
{
    [Export]
    public int moneyCount {get; set;}   
    [Export]
    public string traderCode {get; set;}
    [Export]
    public Array<string> startItemCodes {get; set;} = new Array<string>();
    [Export]
    public Dictionary<string, int> startAmmoCount {get; set;} = new Dictionary<string, int>();

    public new Array<string> itemCodes {get; set;} = new Array<string>();
    public new Dictionary<string, int> ammoCount {get; set;} = new Dictionary<string, int>();
    public Dictionary<string, ItemIcon> ammoButtons {get; set;} = new Dictionary<string, ItemIcon>();
    public Dictionary<int, string> itemPositions {get; set;} = new Dictionary<int, string>();

    public override void _Ready()
    {
        base._Ready();
        BaseTrading.LoadTradingData(this);
    }
    
    public void StartTrading()
    {
        BaseTrading.StartTrading();
    }

    public void StopTrading()
    {
        BaseTrading.StopTrading();
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        BaseTrading.LoadData(data);
    }
    
    public override Dictionary GetSaveData()
    {
        Dictionary saveData = base.GetSaveData();
        Dictionary tradeSaveData = BaseTrading.GetSaveData();
        return Global.MergeDictionaries(saveData, tradeSaveData);
    }
}