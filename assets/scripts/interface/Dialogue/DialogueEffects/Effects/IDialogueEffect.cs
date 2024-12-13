namespace DialogueEffects;

public interface IDialogueEffect
{
    public bool NeedToEnable(string characterName);

    public string GetText(string inputText);

    public string GetConfig();
}
