using Godot;

namespace DialogueScripts;

//Скрипт, запускающий торговлю с неписем, если он реализует интерфейс ITrader
public class DialogueTrade : BaseChangeInNPC
{
    public override async void initiate(Node node, string parameter, string key = "")
    {
        var npc = GetNPC(node);
        if (npc?.GetNodeOrNull<NpcTrading>("trading") is not ITrader trading) return;
        
        if (DialogueMenu.GetNode<Control>("../").Visible)
        {
            //ожидание закрытия диалогового меню
            await node.ToSignal(DialogueMenu, nameof(DialogueMenu.FinishTalking));
        }
        
        // ожидание прорисовки нового кадра, дабы menu manager или subtitles очистились
        await node.ToSignal(node.GetTree(), "idle_frame");
        
        trading.StartTrading();
    }
}