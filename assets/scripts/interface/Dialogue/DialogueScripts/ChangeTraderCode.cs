using Godot;

namespace DialogueScripts;

// меняет имя торговца в заголовке меню торговли
public class ChangeTraderCode : BaseChangeInNPC
{
    public override void initiate(Node node, string parameter, string key = "")
    {
        var npc = GetNPC(node);
        if (npc?.GetNodeOrNull<NpcTrading>("trading") is ITrader trading)
        {
            trading.traderCode = parameter;
        }
    }
}
