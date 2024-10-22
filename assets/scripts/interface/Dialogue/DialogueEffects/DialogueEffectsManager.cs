using Godot;
using System.Linq;
using DialogueEffects;
using Godot.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class DialogueEffectsManager
{
    private static List<IDialogueEffect> effects =
    [
        new WeaponInMouthEffect(),
        new RageEffect()
    ];

    public static string GetTextWithEffects(string inputText, string characterName)
    {
        if (string.IsNullOrEmpty(inputText)) return inputText;

        return effects.Where(effect => effect.NeedToEnable(characterName))
            .Aggregate(inputText, (current, effect) => ApplyEffect(effect, current));
    }

    public static string GetCustomAudioConfig(string characterName)
    {
        return (from effect in effects
                where effect.NeedToEnable(characterName) && effect.GetConfig() != null
                select effect.GetConfig())
            .FirstOrDefault();
    }

    private static string ApplyEffect(IDialogueEffect effect, string inputText)
    {
        var result = "";
        var textParts = DivideTextByBraces(inputText);
        
        foreach (var part in textParts)
        {
            if (part.BeginsWith("["))
            {
                result += part;
            }
            else
            {
                result += effect.GetText(part);
            }
        }

        return result;
    }
    
    private static Array<string> DivideTextByBraces(string inputText)
    {
        var result = new Array<string>();
        const string pattern = @"\[[^\[\]]*\]|[^\[\]]+";
        
        foreach (Match m in Regex.Matches(inputText, pattern))
        {
            result.Add(m.Value);
        }

        return result;
    }
}
