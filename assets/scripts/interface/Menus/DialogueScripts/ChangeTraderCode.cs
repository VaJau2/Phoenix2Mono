using Godot;

namespace DialogueScripts;

public class ChangeTraderCode : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        if (GetNPC(node) is ITrader trader)
        {
            trader.traderCode = parameter;
        }
    }
}
