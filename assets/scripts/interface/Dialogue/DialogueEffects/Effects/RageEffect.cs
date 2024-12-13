using System.Linq;

namespace DialogueEffects;

public class RageEffect : IDialogueEffect
{
    public bool NeedToEnable(string characterName)
    {
        if (characterName != "strikely") return false;

        var player = Global.Get().player;
        var effects = player.Inventory.effects;

        return effects.tempEffects.Any(effect => effect.GetType() == typeof(Effects.RageEffect));
    }

    public string GetText(string inputText)
    {
        return inputText.ToUpper();
    }

    public string GetConfig()
    {
        return "strikely_rage";
    }
}
