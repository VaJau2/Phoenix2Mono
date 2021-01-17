using Godot.Collections;

public static class ItemJSON
{
    static string tempLang;
    static Dictionary itemsData = new Dictionary();
    public static Dictionary GetItemData(string itemCode)
    {
        string lang = InterfaceLang.GetLang();

        if (tempLang != lang) {
            string path = "assets/lang/" + lang + "/items.json";
            itemsData = Global.loadJsonFile(path);
            tempLang = lang;
        }
        
        if (itemsData.Contains(itemCode)) {
            return (Dictionary)itemsData[itemCode];
        } else {
            return new Dictionary();
        }
    }
}