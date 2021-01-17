using Godot;
using Godot.Collections;

public class PlayerInventory 
{
    Player player;
    public Array<string> Items = new Array<string>();

    public string weapon = "";
    public string cloth = "empty";
    public string artifact = "";

    public void SetPlayer(Player player) {
        this.player = player;
    }

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

    public bool itemIsUsable(string itemType) {
        return itemType != "staff";
    }

    public void UseItem(int itemNumber, Dictionary itemData)
    {
        if(itemData.Contains("sound")) {
            string path = "res://assets/audio/item/" + itemData["sound"].ToString() + ".wav";
            var sound = GD.Load<AudioStreamSample>(path);
            
            player.GetAudi().Stream = sound;
            player.GetAudi().Play();
        }
        Items.RemoveAt(itemNumber);
    }

    public void DropItem(int itemNumber)
    {
        GD.Print("dropping item...");
    }
}
