using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class PlayerInventory {
    EffectHandler effects;
    Messages messages;
    Player player;
    public int money = 0;
    public string weapon = "";
    public string cloth = "empty";
    public string artifact = "";

    private Array<string> tempKeys = new Array<string>();

    //ссылки на кнопки с патронами, чтоб было проще их достать при необходимости
    public Dictionary<string, ItemIcon> ammoButtons = new Dictionary<string, ItemIcon>();
    
    private InventoryMenu menu => player.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");

    public PlayerInventory(Player player) 
    {
        this.player = player;
        this.messages = player.GetNode<Messages>("/root/Main/Scene/canvas/messages");
        this.effects = player.GetNode<EffectHandler>("/root/Main/Scene/canvas/effects");
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
                Effect newEffect = EffectHandler.GetEffectByName(itemData["medsEffect"].ToString());
                effects.AddEffect(newEffect);
                messages.ShowMessage("useItem", itemData["name"].ToString(), "items");
                break;
        }
    }

    public void WearItem(string itemCode, bool sound = true)
    {
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        if (sound) {
            SoundUsingItem(itemData);
            messages.ShowMessage("wearItem", itemData["name"].ToString(), "items");
        }

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

    public void MessageCantSell(string itemName)
    {
        messages.ShowMessage("cantSell", itemName, "items");
    }

    public void MessageCantDrop(string itemName)
    {
        messages.ShowMessage("cantDrop", itemName, "items");
    }

    public void MessageCantUnwear(string itemName) 
    {
        messages.ShowMessage("cantUnwear", itemName, "items");
    }

    public void ItemsMessage(string item)
    {
        messages.ShowMessage(item, "items", 2.5f);
    }

    public void LoadItems(Array<string> items, Dictionary<string, int> ammo) 
    {
        menu.LoadItemButtons(items, ammo);
    }
    
    private ItemIcon GetWearButton(string wearButtonName)
    {
        const string buttonsPath = "/root/Main/Scene/canvas/inventory/helper/back/wearBack/";
        return player.GetNode<ItemIcon>(buttonsPath + wearButtonName);
    }

    public Dictionary GetSaveData(bool saveEffects = true)
    {
        var itemCodes = new Array();
        var itemCounts = new Array();
        var itemBinds = new Array();
        foreach (ItemIcon button in menu.mode.itemButtons)
        {
            itemCodes.Add(button.myItemCode ?? "_");
            itemCounts.Add(button.GetCount());
            itemBinds.Add(button.GetBindKey());
        }

        var effectNames = new Array();
        var effectTimes = new Array();
        if (saveEffects)
        {
            foreach (Effect tempEffect in effects.tempEffects)
            {
                effectNames.Add(EffectHandler.GetNameByEffect(tempEffect));
                effectTimes.Add(tempEffect.time);
            }
        }

        var weaponButton = GetWearButton("weapon");
        return new Dictionary()
        {
            {"money", money},
            {"weapon", weapon},
            {"cloth", cloth},
            {"artifact", artifact},
            {"itemCodes", itemCodes},
            {"itemCounts", itemCounts},
            {"itemBinds", itemBinds},
            {"weaponBind", weaponButton.GetBindKey()},
            {"effectNames", effectNames},
            {"effectTimes", effectTimes},
        };
    }

    public void LoadWearItem(string item, string button)
    {
        WearItem(item, false);
        var wearButton = GetWearButton(button);
        wearButton.SetItem(item);
    }
    
    public void LoadData(Dictionary data)
    {
        //загрузка кнопок вещей
        Array itemCodes = (Array) data["itemCodes"];
        Array itemCounts = (Array) data["itemCounts"];
        Array itemBinds = (Array) data["itemBinds"];
        var bindsList = player.GetNode<BindsList>("/root/Main/Scene/canvas/binds");
        for (int i = 0; i < itemCodes.Count; i++)
        {
            var itemCode = itemCodes[i].ToString();
            if (itemCode == "_") continue;
            
            var itemCount = Convert.ToInt32(itemCounts[i]);
            ItemIcon tempButton = menu.mode.itemButtons[i];
            tempButton.SetItem(itemCode);
            tempButton.SetCount(itemCount, false);
            if (itemBinds[i] != null && itemBinds[i].ToString() != "")
            {
                tempButton.SetBindKey(itemBinds[i].ToString());
                menu.bindedButtons[Convert.ToInt32(itemBinds[i])] = tempButton;
                bindsList.AddIcon(tempButton);
            }
        }

        string weaponBind = data["weaponBind"].ToString();
        if (!string.IsNullOrEmpty(weaponBind))
        {
            var weaponButton = GetWearButton("weapon");
            weaponButton.SetBindKey(weaponBind);
            menu.bindedButtons[Convert.ToInt32(weaponBind)] = weaponButton;
        }
        
        //загрузка денег и надетых вещей
        money = Convert.ToInt32(data["money"]);
        
        weapon = data["weapon"].ToString();
        cloth = data["cloth"].ToString();
        artifact = data["artifact"].ToString();
        
        if (!string.IsNullOrEmpty(weapon)) LoadWearItem(weapon, "weapon");
        if (!string.IsNullOrEmpty(cloth) && cloth != "empty") LoadWearItem(cloth, "armor");
        if (!string.IsNullOrEmpty(artifact)) LoadWearItem(artifact, "artifact");

        //загрузка эффектов
        Array effectNames = (Array) data["effectNames"];
        Array effectTimes = (Array) data["effectTimes"];
        for (int i = 0; i < effectNames.Count; i++)
        {
            Effect newEffect = EffectHandler.GetEffectByName(effectNames[i].ToString());
            effects.AddEffect(newEffect);
            newEffect.time = Convert.ToInt32(effectTimes[i]);
        }
    }

    private void SoundUsingItem(Dictionary itemData) 
    {
        if(itemData.Contains("sound")) {
            string path = "res://assets/audio/item/" + itemData["sound"] + ".wav";
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