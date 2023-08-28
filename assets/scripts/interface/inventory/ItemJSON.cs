using System;
using Godot.Collections;

public static class ItemJSON
{
    static string tempLang;
    static Dictionary itemsData = new Dictionary();
    
    public static Dictionary GetItemData(string itemCode)
    {
        string lang = InterfaceLang.GetLang();

        if (tempLang != lang) 
        {
            string path = "assets/lang/" + lang + "/items.json";
            itemsData = Global.loadJsonFile(path);
            tempLang = lang;
        }

        if (!itemsData.Contains(itemCode)) return new Dictionary();
        
        var itemData = (Dictionary)itemsData[itemCode];
        ConvertItemType(itemData);
        
        return itemData;
    }

    private static void ConvertItemType(Dictionary itemData)
    {
        if (!itemData.Contains("type")) return;
        
        string type = itemData["type"].ToString();
        
        if (Enum.TryParse(type, out ItemType itemType))
        {
            itemData["type"] = itemType;
        }
    }
}