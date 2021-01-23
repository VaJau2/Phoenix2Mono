using Godot;
using Godot.Collections;

public class PlayerInventory {
    Messages messages;
    Player player;
    public string weapon = "";
    public string cloth = "empty";
    public string artifact = "";

    private Array<string> tempKeys = new Array<string>();

    public PlayerInventory(Player player, Messages messages) 
    {
        this.player = player;
        this.messages = messages;
    }

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

    public void UseItem(Dictionary itemData)
    {
        SoundUsingItem(itemData);

        switch(itemData["type"]) {
            case "food":
                player.HealHealth(int.Parse(itemData["heal"].ToString()));
                messages.ShowMessage("useFood", itemData["name"].ToString(), "items");
                break;
        }
    }

    public void WearItem(string itemCode)
    {
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        SoundUsingItem(itemData);

        messages.ShowMessage("wearItem", itemData["name"].ToString(), "items");

        switch(itemData["type"]) {
            case "armor":
                cloth = itemCode;
                player.LoadBodyMesh();
                CheckSpeed(itemData);
                break;
        }
    }

    public void UnwearItem(string itemCode, bool changeModel = true)
    {
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        SoundUsingItem(itemData);

        messages.ShowMessage("unwearItem", itemData["name"].ToString(), "items");

        switch(itemData["type"]) {
            case "armor":
                cloth = "empty";
                CheckSpeed(itemData, -1);
                if (changeModel) player.LoadBodyMesh();
                break;
        }
    }

    public void LoadItems(Array<string> items) 
    {
        var menu = player.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        menu.LoadItemButtons(items);
    }

    private void SoundUsingItem(Dictionary itemData) 
    {
        if(itemData.Contains("sound")) {
            string path = "res://assets/audio/item/" + itemData["sound"].ToString() + ".wav";
            var sound = GD.Load<AudioStreamSample>(path);
            
            player.GetAudi().Stream = sound;
            player.GetAudi().Play();
        }
    }

    private void CheckSpeed(Dictionary effects, int factor = 1)
    {
        if (effects.Contains("speedDecrease")) {
            string speedEffect = effects["speedDecrease"].ToString();
            player.BaseSpeed -= int.Parse(speedEffect) * factor;
        }
    }
}