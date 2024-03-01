//Скрипт, запускающий торговлю с неписем, если он реализует интерфейс ITrader
namespace DialogueScripts
{
    public partial class Dialogue_Trade : IDialogueScript
    {
        public async void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            if (dialogueMenu.npc is ITrader trader)
            {
                //ждем, когда закроется диалоговое меню, а потом еще кадр, чтобы menu manager очистился
                await dialogueMenu.ToSignal(dialogueMenu, nameof(DialogueMenu.FinishTalking));
                await dialogueMenu.ToSignal(dialogueMenu.GetTree(), "process_frame");
                trader.StartTrading();
            }
        }
    }
}