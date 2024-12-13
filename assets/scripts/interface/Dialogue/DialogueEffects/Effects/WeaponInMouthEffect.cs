using System.Linq;

namespace DialogueEffects;

public class WeaponInMouthEffect : IDialogueEffect
{
    private System.Collections.Generic.Dictionary<string, string> replaceLetters = new()
    {
        { "т", "ф" }, { "л", "в" }, { "з", "в" }, { "р", "в" }, 
        { "б", "бв" }, { "с", "ф" }, { "щ", "ф" },
        { "х", "ф" }, { "ц", "ф" }, { "ч", "ф" },
        
        { "Т", "Ф" }, { "Л", "В" }, { "З", "В" }, { "Р", "В" }, 
        { "Б", "Бв" }, { "С", "Ф" }, { "Щ", "Ф" },
        { "Х", "Ф" }, { "Ц", "Ф" },  { "Ч", "Ф" },
        
        { "l", "w" }, { "h", "f" }, { "b", "f" },
        { "'t", "'f" }, { "r", "v" }, { "s", "f" },
        
        { "L", "W" }, { "H", "F" }, { "B", "F" },
        { "R", "V" }, { "S", "F" }
    };
    
    public bool NeedToEnable(string characterName)
    {
        if (characterName != "strikely") return false;
        
        var weapons = Global.Get().player.Weapons;
        var playerRace = Global.Get().playerRace;

        return playerRace != Race.Unicorn
               && weapons.GunOn 
               && weapons.isPistol;
    }

    public string GetText(string inputText)
    {
        return replaceLetters.Aggregate(inputText, (current, value) =>
            current.Replace(value.Key, value.Value));
    }

    public string GetConfig()
    {
        return null;
    }
}
