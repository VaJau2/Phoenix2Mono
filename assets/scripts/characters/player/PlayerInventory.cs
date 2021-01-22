using Godot;
using Godot.Collections;

public class PlayerInventory {
    Player player;
    public string weapon = "";
    public string cloth = "empty";
    public string artifact = "";

    private Array<string> tempKeys = new Array<string>();

    public void AddKey(string key) 
    {
        if (!tempKeys.Contains(key)) {
            tempKeys.Add(key);
        }
    }

    public void RemoveKey(string key) {
        if (tempKeys.Contains(key)) {
            tempKeys.Remove(key);
        }
    }

    public Array<string> GetKeys() => tempKeys;


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
    
    public void UseItem(string itemCode)
    {
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        if(itemData.Contains("sound")) {
            string path = "res://assets/audio/item/" + itemData["sound"].ToString() + ".wav";
            var sound = GD.Load<AudioStreamSample>(path);
            
            player.GetAudi().Stream = sound;
            player.GetAudi().Play();
        }
    }

    public void LoadItems(Player player, Array<string> items) 
    {
        this.player = player;
        var menu = player.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        menu.LoadItemButtons(items);
    }
}