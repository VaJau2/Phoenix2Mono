using Godot.Collections;

//Содержит глобальные методы для работы со словарями
public static class DictionaryHelper
{
    public static Dictionary Merge(Dictionary dictA, Dictionary dictB)
    {
        var newDict = dictA.Duplicate();

        if (dictB == null || dictB.Count == 0) return newDict;
        
        foreach (string key in dictB.Keys)
        {
            newDict[key] = dictB[key];
        }

        return newDict;
    }
}
