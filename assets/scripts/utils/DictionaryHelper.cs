using Godot.Collections;

//Содержит глобальные методы для работы со словарями
public static class DictionaryHelper
{
    public static Dictionary Merge(Dictionary dictA, Dictionary dictB)
    {
        var newDict = dictA.Duplicate();

        foreach (string key in dictB.Keys)
        {
            newDict[key] = dictB[key];
        }

        return newDict;
    }

    public static void Merge(ref Dictionary dictA, Dictionary dictB)
    {
        foreach (string key in dictB.Keys)
        {
            dictA[key] = dictB[key];
        }
    }
}