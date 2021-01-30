using Godot;
using Godot.Collections;

public class PlayerInventory {
    EffectHandler effects;
    Messages messages;
    Player player;
    public string weapon = "";
    public string cloth = "empty";
    public string artifact = "";

    private Array<string> tempKeys = new Array<string>();

    //ссылки на кнопки с патронами, чтоб было проще их достать при необходимости
    private Dictionary<string, ItemIcon> ammoButtons = new Dictionary<string, ItemIcon>();

    public PlayerInventory(Player player) 
    {
        this.player = player;
        this.messages = player.GetNode<Messages>("/root/Main/Scene/canvas/messages");
        this.effects = player.GetNode<EffectHandler>("/root/Main/Scene/canvas/effects");
    }

    public ItemIcon GetAmmoButton(string ammoType) 
    {
        if (ammoButtons.ContainsKey(ammoType)) {
            return ammoButtons[ammoType];
        } else {
            return null;
        }
    }
    
    public void SetAmmoButton(string ammoType, ItemIcon button)
    {
        if (ammoButtons.ContainsKey(ammoType)) {
            ammoButtons[ammoType] = button;
        } else {
            ammoButtons.Add(ammoType, button);
        }
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
        return itemType != "staff" && itemType != "ammo";
    }

    public void UseItem(Dictionary itemData)
    {
        SoundUsingItem(itemData);

        switch(itemData["type"]) {
            case "food":
                if (player.FoodCanHeal)
                    player.HealHealth(int.Parse(itemData["heal"].ToString()));
                messages.ShowMessage("useFood", itemData["name"].ToString(), "items");
                break;
            case "meds":
                Effect newEffect = effects.GetEffectByName(itemData["medsEffect"].ToString());
                effects.AddEffect(newEffect);
                messages.ShowMessage("useItem", itemData["name"].ToString(), "items");
                break;
        }
    }

    public void WearItem(string itemCode)
    {
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        SoundUsingItem(itemData);

        messages.ShowMessage("wearItem", itemData["name"].ToString(), "items");

        switch(itemData["type"]) {
            case "weapon":
                weapon = itemCode;
                player.Weapons.LoadNewWeapon(itemCode, itemData);
                break;
            case "armor":
                cloth = itemCode;
                player.LoadBodyMesh();
                CheckSpeed(itemData);
                break;
            case "artifact":
                artifact = itemCode;
                player.LoadArtifactMesh(itemCode);
                break;
        }
    }

    public void UnwearItem(string itemCode, bool changeModel = true)
    {
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        SoundUsingItem(itemData);

        messages.ShowMessage("unwearItem", itemData["name"].ToString(), "items");

        switch(itemData["type"]) {
            case "weapon":
                weapon = "";
                player.Weapons.ClearWeapon();
                break;
            case "armor":
                cloth = "empty";
                CheckSpeed(itemData, -1);
                if (changeModel) player.LoadBodyMesh();
                break;
            case "artifact":
                artifact = "";
                if (changeModel) player.LoadArtifactMesh();
                break;
        }
    }

    public void MessageCantUnwear(string itemName) 
    {
        messages.ShowMessage("cantUnwear", itemName, "items");
    }

    public void MessageNotEnoughSpace()
    {
        messages.ShowMessage("notSpace", "items", 2.5f);
    }

    public void LoadItems(Array<string> items, Dictionary<string, int> ammo) 
    {
        var menu = player.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        menu.LoadItemButtons(items, ammo);
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