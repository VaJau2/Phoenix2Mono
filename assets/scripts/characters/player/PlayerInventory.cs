using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class PlayerInventory 
{
    public EffectHandler effects;
    Messages messages;
    Player player;
    public int money = 0;
    public string weapon = "";
    public string cloth = "empty";
    public string artifact = "";

    private Array<string> tempKeys = new Array<string>();

    //ссылки на кнопки с патронами, чтоб было проще их достать при необходимости
    public Dictionary<string, ItemIcon> ammoButtons = new Dictionary<string, ItemIcon>();
    
    public InventoryMenu menu => player.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");

    private string tempClothDataName;
    private Dictionary tempClothData;

    private string tempWeaponDataName;
    private Dictionary tempWeaponData;

    public PlayerInventory(Player player) 
    {
        this.player = player;
        messages = player.GetNode<Messages>("/root/Main/Scene/canvas/messages");
        effects = player.GetNode<EffectHandler>("/root/Main/Scene/canvas/effects");
    }

    public bool HasItem(string itemCode)
    {
        return menu.mode.FindButtonWithItem(itemCode) != null;
    }
    
    public void SetAmmoButton(string ammoType, ItemIcon button)
    {
        if (ammoButtons.ContainsKey(ammoType)) 
        {
            ammoButtons[ammoType] = button;
        } 
        else 
        {
            ammoButtons.Add(ammoType, button);
        }
    }

    public void AddKey(string key) 
    {
        if (!tempKeys.Contains(key)) 
        {
            tempKeys.Add(key);
        }
    }

    public void RemoveKey(string key) 
    {
        if (menu.mode.SameItemCount(key) > 1)
        {
            return;
        }
        
        if (tempKeys.Contains(key)) 
        {
            tempKeys.Remove(key);
        }
    }

    public void SetBindsCooldown(float cooldown)
    {
        menu.SetBindsCooldown(cooldown);
    }

    public Array<string> GetKeys() => tempKeys;
    
    public Dictionary GetArmorProps() 
    {
        if (cloth == tempClothDataName) return tempClothData;
        
        tempClothData = ItemJSON.GetItemData(cloth);
        tempClothDataName = cloth;

        return tempClothData;
    }

    public Dictionary GetWeaponProps()
    {
        if (weapon == tempWeaponDataName) return tempWeaponData;
        
        tempWeaponData = ItemJSON.GetItemData(weapon);
        tempWeaponDataName = weapon;

        return tempWeaponData;
    }

    public bool itemIsUsable(ItemType itemType) 
    {
        return itemType != ItemType.staff && itemType != ItemType.ammo && itemType != ItemType.money;
    }

    public void UseItem(Dictionary itemData)
    {
        SoundUsingItem(itemData);

        switch((ItemType)itemData["type"]) 
        {
            case ItemType.food:
                if (player.FoodCanHeal)
                {
                    player.HealHealth(int.Parse(itemData["heal"].ToString()));
                }
                messages.ShowMessage("useFood", itemData["name"].ToString(), "items");
                break;
            case ItemType.meds:
                Effect newEffect = EffectHandler.GetEffectByName(itemData["medsEffect"].ToString());
                effects.AddEffect(newEffect);
                messages.ShowMessage("useItem", itemData["name"].ToString(), "items");
                break;
        }
    }

    private void CheckStealthBuck()
    {
        Effect stealthBuckEffect = effects.GetTheSameEffect(new StealthBuckEffect());
        stealthBuckEffect?.SetOff(false);
    }

    public bool CheckCanWearItem(string itemCode)
    {
        Dictionary itemData = ItemJSON.GetItemData(itemCode);

        //если предмет только для земных пней
        if (itemData.Contains("onlyForEarthponies")
            && Global.Get().playerRace != Race.Earthpony)
        {
            return false;
        }

        //если для предмета требуется наличие другого предмета
        if (itemData.Contains("checkHasItem"))
        {
            string checkItem = itemData["checkHasItem"].ToString();
            if (!HasItem(checkItem)) return false;
        }

        return true;
    }

    public void WearItem(string itemCode, bool sound = true)
    {
        CheckStealthBuck();
        
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        if (sound) 
        {
            SoundUsingItem(itemData);
            messages.ShowMessage("wearItem", itemData["name"].ToString(), "items");
        }

        switch((ItemType)itemData["type"]) 
        {
            case ItemType.weapon:
                weapon = itemCode;
                player.Weapons.LoadNewWeapon(itemCode, itemData);
                break;
            case ItemType.armor:
                cloth = itemCode;
                player.LoadBodyMesh();
                CheckSpeed(itemData);
                break;
            case ItemType.artifact:
                artifact = itemCode;
                player.LoadArtifactMesh(itemCode);
                break;
        }
    }

    public void UnwearItem(string itemCode, bool changeModel = true)
    {
        CheckStealthBuck();
        
        Dictionary itemData = ItemJSON.GetItemData(itemCode);
        SoundUsingItem(itemData);

        messages.ShowMessage("unwearItem", itemData["name"].ToString(), "items");

        switch((ItemType)itemData["type"]) 
        {
            case ItemType.weapon:
                weapon = "";
                player.Weapons.ClearWeapon();
                break;
            
            case ItemType.armor:
                cloth = "empty";
                CheckSpeed(itemData, -1);
                if (changeModel) player.LoadBodyMesh();
                break;
            
            case ItemType.artifact:
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
    
    public void MessageCantWear(string itemName) 
    {
        messages.ShowMessage("cantWear", itemName, "items");
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
    
    public ItemIcon GetWearButton(string wearButtonName)
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
        return new Dictionary
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
            {"tempKeys", tempKeys}
        };
    }

    public void LoadWearItem(string item, string button)
    {
        WearItem(item, false);
        var wearButton = GetWearButton(button);
        wearButton.SetItem(item);
    }

    public void LoadMoney(string loadedMoney)
    {
        money = Convert.ToInt32(loadedMoney);
    }
    
    public void LoadData(Dictionary data)
    {
        if (data.Count == 0) return;
        
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
            
            if (itemBinds.Count == 0) continue;
            
            if (itemBinds[i] != null && itemBinds[i].ToString() != "")
            {
                tempButton.SetBindKey(itemBinds[i].ToString());
                menu.bindedButtons[Convert.ToInt32(itemBinds[i])] = tempButton;
                bindsList.AddIcon(tempButton);
            }
        }

        //загрузка денег и надетых вещей
        LoadMoney(data["money"].ToString());

        weapon = data["weapon"].ToString();
        cloth = data["cloth"].ToString();
        artifact = data["artifact"].ToString();
        
        if (!string.IsNullOrEmpty(weapon)) LoadWearItem(weapon, "weapon");
        if (!string.IsNullOrEmpty(cloth) && cloth != "empty") LoadWearItem(cloth, "armor");
        if (!string.IsNullOrEmpty(artifact)) LoadWearItem(artifact, "artifact");
        
        string weaponBind = data["weaponBind"].ToString();
        if (!string.IsNullOrEmpty(weaponBind))
        {
            var weaponButton = GetWearButton("weapon");
            weaponButton.SetBindKey(weaponBind);
            menu.bindedButtons[Convert.ToInt32(weaponBind)] = weaponButton;
            bindsList.AddIcon(weaponButton);
        }

        if (data.Contains("tempKeys") && data["tempKeys"] is Array)
        {
            foreach (string key in (Array)data["tempKeys"])
            {
                tempKeys.Add(key);
            }
        }
        

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
        if(itemData.Contains("sound")) 
        {
            string path = "res://assets/audio/item/" + itemData["sound"] + ".wav";
            var sound = GD.Load<AudioStreamSample>(path);
            
            player.GetAudi().Stream = sound;
            player.GetAudi().Play();
        }
    }

    private void CheckSpeed(Dictionary effects, int factor = 1)
    {
        if (effects.Contains("speedDecrease")) 
        {
            string speedEffect = effects["speedDecrease"].ToString();
            player.BaseSpeed -= int.Parse(speedEffect) * factor;
        }
    }
}