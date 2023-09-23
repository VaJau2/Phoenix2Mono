namespace DialogueScripts
{
    public class ChangeTraderCode : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            if (dialogueMenu.npc is ITrader trader)
            {
                trader.traderCode = parameter;
            }
        }
    }
}
