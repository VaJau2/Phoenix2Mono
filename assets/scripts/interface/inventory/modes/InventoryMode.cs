using System.Linq;
using Godot;
using Godot.Collections;

public abstract class InventoryMode
{
    private const float MENU_SPEED = 16f;
    private const float MENU_SIZE = 272f;
    private const float DEAD_ZONE = 10f;

    protected Player player => Global.Get().player;
    protected InventoryMenu menu;
    protected Control wearBack;
    protected Control back;
    protected Label moneyCount;

    public UseHandler useHandler { get; }
    public BagSpawner bagSpawner { get; }
    protected BindsHandler bindsHandler;

    protected Control itemInfo;
    protected Label itemName;
    protected Label itemDesc;
    protected Label itemProps;
    protected ControlHintsManager controlHints;

    public bool isAnimating => anim.IsPlaying();
    private bool isOpening;
    private AnimationPlayer anim;
    
    public bool ModalOpened => useHandler.ModalOpened;
    public Control modalAsk { get; }
    public Control modalRead { get; }

    public ItemIcon tempButton { get; private set; }
    public Dictionary tempItemData { get; private set; }
    public Array<ItemIcon> itemButtons {get;} = new Array<ItemIcon>();
    
    //key = key, value = button
    protected Dictionary<string, Label> labels = new Dictionary<string, Label>();

    private Vector2 savedMousePos;
    
    public bool isDragging { get; private set; }
    protected IconWithShadow dragIcon;
    readonly Vector2 dragIconOffset = new Vector2(21, 21);

    protected PlayerInventory inventory => player.Inventory;

    private readonly object syncLocker = new();

    public InventoryMode(InventoryMenu menu)
    {
        this.menu = menu;

        foreach(object button in menu.GetNode<Control>("helper/back/items").GetChildren()) 
        {
            itemButtons.Add(button as ItemIcon); 
        }
        
        if (!menu.menuLoaded && Global.Get().playerRace != Race.Earthpony) 
        {
            for(int i = 0; i < 5; i++) 
            {
                itemButtons[itemButtons.Count - 1].QueueFree();
                itemButtons.RemoveAt(itemButtons.Count - 1);
            }
            menu.menuLoaded = true;
        }

        var back = menu.GetNode<Control>("helper/back");
        wearBack = back.GetNode<Control>("wearBack");
        moneyCount = back.GetNode<Label>("moneyCount");
        modalAsk   = menu.GetNode<Control>("modalAsk");
        modalRead  = menu.GetNode<Control>("modalRead");

        itemInfo = back.GetNode<Control>("itemInfo");
        itemName = itemInfo.GetNode<Label>("name");
        itemDesc = itemInfo.GetNode<Label>("description");
        itemProps = itemInfo.GetNode<Label>("props");
        controlHints = itemInfo.GetNode<ControlHintsManager>("hints");
        dragIcon = back.GetNode<IconWithShadow>("dragIcon");

        labels.Add("money", back.GetNode<Label>("moneyLabel"));
        labels.Add("wear", back.GetNode<Label>("wearBack/Label"));
        labels.Add("weapon", back.GetNode<Label>("wearBack/weaponLabel"));
        labels.Add("armor", back.GetNode<Label>("wearBack/armorLabel"));
        labels.Add("artifact", back.GetNode<Label>("wearBack/artifactLabel"));

        anim = menu.GetNode<AnimationPlayer>("anim");
        
        bindsHandler = new BindsHandler(menu, this);
        useHandler = new UseHandler(menu, this, bindsHandler);
        bagSpawner = new BagSpawner();
        
        if (!anim.IsConnected("animation_finished", menu, nameof(InventoryMenu.OpenAnimFinished)))
        {
            anim.Connect("animation_finished", menu, nameof(InventoryMenu.OpenAnimFinished));
        }
    }
    
    public ItemIcon FindButtonWithItem(string itemCode)
    {
        return itemButtons.FirstOrDefault(button => button.myItemCode == itemCode);
    }

    public int SameItemCount(string itemCode)
    {
        return itemButtons.Count(inventoryButton => inventoryButton.myItemCode == itemCode);
    }

    public void LoadItemButtons(Array<string> newItems, Dictionary<string, int> ammo)
    {
        foreach (var item in newItems)
        {
            AddNewItem(item);
        }

        foreach (string ammoItem in ammo.Keys) 
        {
            ItemIcon newAmmoButton = AddNewItem(ammoItem);
            newAmmoButton.SetCount(ammo[ammoItem]);
        }
    }

    public void SetTempButton(ItemIcon newButton, bool showInfo = true)
    {
        lock (syncLocker)
        {
            tempButton = newButton;
            if (newButton != null)
            {
                var itemCode = newButton.myItemCode;
                if (string.IsNullOrEmpty(itemCode)) return;
            
                tempItemData = ItemJSON.GetItemData(itemCode);
                if (showInfo) 
                {
                    itemInfo.Visible = true;
                    itemName.Text = tempItemData["name"].ToString();
                    itemDesc.Text = tempItemData["description"].ToString();
                    itemProps.Text = GetItemPropsString(tempItemData);
                    LoadControlHint(newButton.isInventoryIcon);
                }
            } 
            else 
            {
                itemInfo.Visible = false;
                tempItemData = null;
            }
        }
    }

    public void UseTempItem()
    {
        useHandler.HideLoadingIcon();
        useHandler.UseTempItem();
    }

    //грузим подсказки по управлению предметом
    protected virtual void LoadControlHint(bool isInventoryIcon)
    {
        var type = (ItemType)tempItemData["type"];
        ControlText[] controlTexts;

        switch (type)
        {
            case ItemType.weapon:
                controlTexts = new[] { ControlText.equip, ControlText.bind, ControlText.move, ControlText.drop };
                break;

            case ItemType.armor:
            case ItemType.artifact:
                controlTexts = new[] { ControlText.equip, ControlText.move, ControlText.drop };
                break;

            case ItemType.note:
                controlTexts = new[] { ControlText.read, ControlText.move, ControlText.drop };
                break;

            case ItemType.food:
                controlTexts = new[] { ControlText.eat, ControlText.bind, ControlText.move, ControlText.drop };
                break;

            case ItemType.meds:
                controlTexts = new[] { ControlText.use, ControlText.bind, ControlText.move, ControlText.drop };
                break;

            default:
                controlTexts = new[] { ControlText.move, ControlText.drop };
                break;
        }

        controlHints.LoadHits(controlTexts);
    }

    public ItemIcon FirstEmptyButton
    {
        get { return itemButtons.FirstOrDefault(button => button.myItemCode == null); }
    }

    private ItemIcon AddNewItem(string itemCode) 
    {
        ItemIcon emptyButton = FirstEmptyButton;
        if (emptyButton != null) 
        {
            emptyButton.SetItem(itemCode);

            if (itemCode.Contains("key")) 
            {
                inventory.AddKey(itemCode);
            }
        } 
        else 
        {
            inventory.ItemsMessage("space");
        }
        
        return emptyButton;
    }

    public virtual void RemoveItemFromButton(ItemIcon button)
    {
        if (button.myItemCode.Contains("key")) 
        {
            inventory.RemoveKey(button.myItemCode);
        }
        bindsHandler.ClearBind(button);
        button.ClearItem();
    }
   
    public void RemoveTempItem()
    {
        if (tempButton.myItemCode.Contains("key")) 
        {
            inventory.RemoveKey(tempButton.myItemCode);
        }
        RemoveItemFromButton(tempButton);
        tempButton = null;
    }

    protected static string ConvertPropValue(string propName, string propValue)
    {
        switch (propName)
        {
            case "damage":
                //добавляем настройки урона игрока в выводе урона от оружия
                float damageValue = Global.ParseFloat(propValue) * Global.Get().Settings.playerDamage;
                int intDamage = (int) damageValue;
                return intDamage.ToString();
            
            case "medsEffect":
                return InterfaceLang.GetPhrase("inventory", "medsEffects", propValue);
                
            case "damageBlock":
                //выводим блокирование урона в процентах
                float blockValue = Global.ParseFloat(propValue);
                return (blockValue * 100f) + "%";
                
            case "checkHasItem":
                var itemData = ItemJSON.GetItemData(propValue);
                return itemData["name"].ToString();
                
            case "questItem": 
            case "ignoreRadiation":
            case "onlyForEarthponies":
                return "";
        }

        return propValue;
    }

    protected virtual string GetItemPropsString(Dictionary itemProps)
    {
        string result = "";
        Dictionary itemPropNames = InterfaceLang.GetPhrasesSection("inventory", "itemProps");
        foreach(string prop in itemProps.Keys) 
        {
            if (itemPropNames.Contains(prop)) 
            {
                // игнор требований силовой брони, дабы её инфа поместилась в меню инвентаря
                if (tempButton.myItemCode is "powerArmor" && (prop is "checkHasItem" or "onlyForEarthponies")) 
                    continue;
                
                string propName = itemPropNames[prop].ToString();
                string propValue = itemProps[prop].ToString();
                propValue = ConvertPropValue(prop, propValue);
                
                result += "> " + propName + propValue + "\n";
            }
        }
        return result;
    }

    protected void CheckTempIcon()
    {
        if (tempButton == null) return;
        tempButton._on_itemIcon_mouse_exited();
        tempButton = null;
    }

    public bool CheckMouseInButton(Control button) 
    {
        var mouse = button.GetLocalMousePosition();
        return mouse.x >= 0 && mouse.x <= button.RectSize.x 
            && mouse.y >= 0 && mouse.y <= button.RectSize.y;
    }

    public virtual void ChangeItemButtons(ItemIcon oldButton, ItemIcon newButton)
    {
        //меняем местами бинды клавиш на кнопках
        string tempBind = oldButton.GetBindKey();
        oldButton.SetBindKey(newButton.GetBindKey());
        newButton.SetBindKey(tempBind);

        if (oldButton.GetBindKey() != "") 
        {
            int keyId = int.Parse(oldButton.GetBindKey());
            menu.bindedButtons[keyId] = oldButton;
        }
        if (newButton.GetBindKey() != "") 
        {
            int keyId = int.Parse(newButton.GetBindKey());
            menu.bindedButtons[keyId] = newButton;
        }

        //меняем местами количество предметов (если это патроны)
        int tempCount = oldButton.GetCount();
        oldButton.SetCount(newButton.GetCount(), false);
        newButton.SetCount(tempCount, false);
        
        //меняем местами вещи на кнопках
        if (newButton.myItemCode == null) 
        {
            var tempItemCode = oldButton.myItemCode;
            oldButton.ClearItem();
            newButton.SetItem(tempItemCode);
        } 
        else 
        {
            var tempItemCode = oldButton.myItemCode;
            oldButton.SetItem(newButton.myItemCode);
            newButton.SetItem(tempItemCode);
        }
    }

    protected virtual void CheckDragItem()
    {
        var itemType = (ItemType)tempItemData["type"];

        switch (itemType)
        {
            case ItemType.weapon when CheckMouseInButton(useHandler.weaponButton):
                useHandler.WearTempItem(useHandler.weaponButton); return;
            case ItemType.armor when CheckMouseInButton(useHandler.armorButton):
                useHandler.WearTempItem(useHandler.armorButton); return;
            case ItemType.artifact when CheckMouseInButton(useHandler.artifactButton):
                useHandler.WearTempItem(useHandler.artifactButton); return;
        }

        foreach (var otherButton in itemButtons)
        {
            var buttonControl = (Control)otherButton;
            if (tempButton == otherButton || !CheckMouseInButton(buttonControl)) continue;
            if (IsUnwearingItem(itemType))
            {
                if (!useHandler.CanTakeItemOff()) return;
                inventory.UnwearItem(tempButton.myItemCode);
            }

            ChangeItemButtons(tempButton, otherButton);
            SetTempButton(otherButton, false);
            dragIcon.SetTexture(null);
        }
    }

    private bool IsUnwearingItem(ItemType itemType)
    {
        return (itemType == ItemType.weapon && tempButton == useHandler.weaponButton)
        || (itemType == ItemType.armor && tempButton == useHandler.armorButton)
        || (itemType == ItemType.artifact && tempButton == useHandler.artifactButton);
    }

    private void LoadLabels()
    {
        foreach(string labelName in labels.Keys) 
        {
            Label tempLabel = labels[labelName];
            tempLabel.Text = InterfaceLang.GetPhrase("inventory", "labels", labelName);
        }
    }
    
    public virtual void OpenMenu()
    {
        isOpening = true;
        player.SetMayMove(false);

        LoadLabels();
        moneyCount.Text = inventory.money.ToString();

        menu.Visible = true;
        wearBack.Visible = true;

        anim.Play("Open");
    }

    public virtual void CloseMenu()
    {
        isOpening = false;
        
        if (isDragging)
        {
            FinishDragging();
        }
        
        CheckTempIcon();
        
        player.SetMayMove(true);
        
        anim.Play("Close");
    }

    public void FinishOpening()
    {
        if (isOpening)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            menu.isOpen = true;
        }
        else
        {
            wearBack.Visible = false;
            menu.Visible = false;
            menu.isOpen = false;
        
            if (!Global.Get().paused) 
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        }
    }
    
    private void FinishDragging()
    {
        tempButton?.SetIcon((StreamTexture) dragIcon.Texture);
        dragIcon.SetTexture(null);
        dragIcon.RectGlobalPosition = Vector2.Zero;
        isDragging = false;
    }

    protected bool UpdateDragging(InputEvent @event)
    {
        var itemType = (ItemType)tempItemData["type"];

        if (Input.IsActionJustPressed("ui_click"))
        {
            if (itemType is ItemType.money or ItemType.ammo && !tempButton.isInventoryIcon)
            {
                MoveTempItem();
                return false;
            }
            
            savedMousePos = menu.GetGlobalMousePosition();
        }
        
        if (Input.IsActionPressed("ui_click"))
        {
            if (MouseIsMoving(@event) && IsMouseOutsideDeadZone() || isDragging)
            {
                if (!isDragging)
                {
                    useHandler.HideLoadingIcon();

                    dragIcon.SetTexture(tempButton.GetIcon());
                    dragIcon.RectGlobalPosition = tempButton.RectGlobalPosition;

                    tempButton.SetIcon(null);
                    isDragging = true;
                }

                dragIcon.RectGlobalPosition = tempButton.GetGlobalMousePosition() - dragIconOffset;
            }
            else
            {
                if (inventory.ItemIsUsable(itemType))
                {
                    useHandler.ShowLoadingIcon();
                }
            }
        }

        if (Input.IsActionJustReleased("ui_click"))
        {
            useHandler.HideLoadingIcon();
            
            if (isDragging)
            {
                FinishDragging();
            }

            if (IsMouseOutsideDeadZone())
            {
                CheckDragItem();
                return true;
            }

            MoveTempItem();
        }

        return false;
    }

    protected bool IsMouseOutsideDeadZone()
    {
        var mousePos = menu.GetGlobalMousePosition();
        return mousePos.DistanceTo(savedMousePos) > DEAD_ZONE;
    }
    
    private bool MouseIsMoving(InputEvent @event)
    {
        return @event is InputEventMouseMotion;
    }
    
    public virtual void CloseModal()
    {
        useHandler.CloseModal();
        
        var updateMenu = true;
        
        foreach (var itemButton in itemButtons)
        {
            itemButton.OnModalClosed(updateMenu);
            
            if (itemButton.IsCursorInside)
            {
                updateMenu = false;
            }
        }
    }

    private void UseBinds()
    {
        if (player == null) return;
        if (!player.MayRotateHead) return;
        if (bindsHandler.useCooldown > 0) return;

        for (var i = 0; i < 10; i++)
        {
            if (Input.IsKeyPressed(48 + i) && menu.bindedButtons.Keys.Contains(i))
            {
                SetTempButton(menu.bindedButtons[i], false);
                useHandler.UseTempItem();
            }
        }
    }

    private void CheckAutoheal()
    {
        if (!Godot.Object.IsInstanceValid(player)) return;
        if (!Input.IsActionJustPressed("autoheal") || !player.MayMove) return;
        if (player.Health == player.HealthMax) 
        {
            inventory.ItemsMessage("youAreHealthy");
            return;
        }

        foreach (var newTempButton in itemButtons)
        {
            if (newTempButton.myItemCode == null) continue;
            SetTempButton(newTempButton);
            if (tempButton.myItemCode != "healPotion" && (ItemType)tempItemData["type"] != ItemType.food) continue;
            useHandler.UseTempItem();
            return;
        }
        inventory.ItemsMessage("cantFindHeal");
        CheckTempIcon();
    }
    
    public virtual void Process(float delta) {}

    protected virtual void MoveTempItem() { }

    public virtual void UpdateInput(InputEvent @event)
    {
        if (@event is InputEventKey && tempButton == null) 
        {
            UseBinds();
            CheckAutoheal();
        }
    }

    public virtual void _on_modal_no_pressed() {}
    public virtual void _on_modal_yes_pressed() {}
    public virtual void _on_count_value_changed(float newCount) { }
    public virtual void _on_takeAll_pressed() {}
}