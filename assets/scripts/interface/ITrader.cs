using Godot.Collections;

public interface ITrader 
{
    int moneyCount {get; set;}
    string traderCode {get; set;}
    Array<string> itemCodes {get; set;}
    Dictionary<string, int> ammoCount {get; set;}
    Dictionary<string, ItemIcon> ammoButtons {get; set;}
    Dictionary<int, string> itemPositions {get; set;}

    void StartTrading();
}