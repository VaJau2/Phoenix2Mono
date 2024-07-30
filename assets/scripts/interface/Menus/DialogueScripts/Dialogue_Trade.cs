using Godot;

namespace DialogueScripts;

//Скрипт, запускающий торговлю с неписем, если он реализует интерфейс ITrader
public class Dialogue_Trade : BaseChangeInNPC
{
    public override async void initiate(Node node, string parameter, string key = "")
    {
        if (GetNPC(node) is not ITrader trader) return;
        
        var dialogueMenu = node.GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");

        if (dialogueMenu.GetNode<Control>("../").Visible)
        {
            //ожидание закрытия диалогового меню
            await node.ToSignal(dialogueMenu, nameof(DialogueMenu.FinishTalking));
        }
        
        // ожидание прорисовки нового кадра, дабы menu manager или subtitles очистились
        await node.ToSignal(node.GetTree(), "idle_frame");
        
        trader.StartTrading();
    }
}