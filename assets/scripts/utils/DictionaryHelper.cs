using Godot.Collections;

public static class DictionaryHelper
{
    public static Dictionary Merge(Dictionary dictA, Dictionary dictB)
    {
        var newDict = new Dictionary(dictA);
        
        foreach (string key in dictB.Keys)
        {
            newDict[key] = dictB[key];
        }

        return newDict;
    }
}