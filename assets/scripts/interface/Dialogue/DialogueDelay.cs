using System;
using System.Globalization;

public class DialogueDelay
{
    public const char DELAY_SYMBOL = '@';
    public const float DEFAULT_SYMBOL_DELAY = 0.03f;
    public const float DEFAULT_PHRASE_DELAY = 1f;
    
    private const float DEFAULT_COMMA_DELAY = 0.25f;
    private const float DEFAULT_PERIOD_DELAY = 0.5f;
    
    public static float Get(ref string phrase, float phraseDelay, float symbolDelay)
    {
        return phrase.Length switch
        {
            <=1 => phraseDelay,
            
            _ => phrase[0] switch
            {
                DELAY_SYMBOL => GetCustomDelay(ref phrase, phraseDelay),

                '.' or '!' or '?' or '…' => phrase.Length > 1 ? DEFAULT_PERIOD_DELAY : phraseDelay,

                ',' => DEFAULT_COMMA_DELAY,

                _ => symbolDelay
            }
        };
    }

    public static string ClearDelaySymbols(string phrase)
    {
        for (var i = 0; i < phrase.Length; i++)
        {
            if (phrase[i] != DELAY_SYMBOL) continue;
            
            phrase = phrase.Remove(i, 1);
            
            if (i >= phrase.Length) return phrase;
            if (!int.TryParse(phrase[i].ToString(), out _)) continue;
            
            while (phrase[i] != DELAY_SYMBOL)
            {
                phrase = phrase.Remove(i, 1);
            }
            
            phrase = phrase.Remove(i, 1);
        }

        return phrase;
    }
    
    private static float GetCustomDelay(ref string phrase, float phraseDelay)
    {
        if (!int.TryParse(phrase[1].ToString(), out _))
        {
            phrase = phrase.Substring(1);
            return phraseDelay;
        }
        
        var delayString = "";
        var i = 1;

        while (phrase[i] != DELAY_SYMBOL)
        {
            delayString += phrase[i];
            i++;
        }

        phrase = phrase.Substring(i+1);
        return Convert.ToSingle(delayString, CultureInfo.InvariantCulture);
    }
}