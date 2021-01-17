using Godot;
using Godot.Collections;

public class PlayerInventory 
{
    public Array<string> Items = new Array<string>();

    public string weapon = "";
    public string cloth = "empty";
    public string artifact = "";

    public Array<string> GetKeys() 
    {
        Array<string> tempKeys = new Array<string>();

        foreach(string item in Items) {
            if (item.Contains("key")) {
                tempKeys.Add(item);
            }
        }

        return tempKeys;
    }

    public Dictionary GetArmorProps() 
    {
        return ItemJSON.GetItemData(cloth);
    }

    public Dictionary GetWeaponProps()
    {
        return ItemJSON.GetItemData(weapon);
    }
}
