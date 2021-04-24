using Godot;
using Godot.Collections;

public abstract class InventoryMode
{
    protected const float MENU_SPEED = 16f;
    protected const float MENU_SIZE = 272f;

    protected Player player => Global.Get().player;
    protected InventoryMenu menu;
    protected BindsList bindsList;
    protected Control back;
    protected Label moneyCount;

    protected Control itemInfo;
    protected Label itemName;
    protected Label itemDesc;
    protected Label itemProps;
    protected Label controlHints;

    public bool isAnimating = false;
    public bool isDragging {get; protected set;} = false;
    public bool ModalOpened {get; protected set;} = false;
    public Control modalAsk {get; protected set;}
    public Control modalRead {get; protected set;}

    protected ItemIcon tempButton;
    protected Dictionary tempItemData;
    public Array<ItemIcon> itemButtons {get;} = new Array<ItemIcon>();
    //key = key, value = button
    protected Dictionary<string, Label> labels = new Dictionary<string, Label>();

    protected float dragTimer = 0;
    protected TextureRect dragIcon;
    protected PlayerInventory inventory => player.inventory;

    public InventoryMode(InventoryMenu menu)
    {
        this.menu = menu;

        foreach(object button in menu.GetNode<Control>("helper/back/items").GetChildren()) {
            itemButtons.Add(button as ItemIcon); 
        }
        
        if (!menu.menuLoaded && Global.Get().playerRace != Race.Earthpony) {
            for(int i = 0; i < 5; i++) {
                itemButtons[itemButtons.Count - 1].QueueFree();
                itemButtons.RemoveAt(itemButtons.Count - 1);
            }
            menu.menuLoaded = true;
        }

        bindsList = menu.GetNode<BindsList>("/root/Main/Scene/canvas/binds");
        back = menu.GetNode<Control>("helper/back");
        moneyCount = back.GetNode<Label>("moneyCount");
        modalAsk   = menu.GetNode<Control>("modalAsk");
        modalRead  = menu.GetNode<Control>("modalRead");

        itemInfo = back.GetNode<Control>("itemInfo");
        itemName = itemInfo.GetNode<Label>("name");
        itemDesc = itemInfo.GetNode<Label>("description");
        itemProps = itemInfo.GetNode<Label>("props");
        controlHints = itemInfo.GetNode<Label>("hints");
        dragIcon = back.GetNode<TextureRect>("dragIcon");

        labels.Add("name", back.GetNode<Label>("Label"));
        labels.Add("money", back.GetNode<Label>("moneyLabel"));
        labels.Add("wear", back.GetNode<Label>("wearBack/Label"));
        labels.Add("weapon", back.GetNode<Label>("wearBack/weaponLabel"));
        labels.Add("armor", back.GetNode<Label>("wearBack/armorLabel"));
        labels.Add("artifact", back.GetNode<Label>("wearBack/artifactLabel"));
    }

    public void LoadItemButtons(Array<string> newItems, Dictionary<string, int> ammo)
    {
        for (int i = 0; i < newItems.Count; i++) {
            AddNewItem(newItems[i]);
        }
        foreach(string ammoItem in ammo.Keys) {
            ItemIcon newAmmoButton = AddNewItem(ammoItem);
            newAmmoButton.SetCount(ammo[ammoItem]);
        }
    }

    public void SetTempButton(ItemIcon newButton, bool showInfo = true)
    {
        tempButton = newButton;
        if (newButton != null) {
            string itemCode = newButton.myItemCode;
            tempItemData = ItemJSON.GetItemData(itemCode);
            if (showInfo) {
                itemInfo.Visible = true;
                itemName.Text = tempItemData["name"].ToString();
                itemDesc.Text = tempItemData["description"].ToString();
                itemProps.Text = GetItemPropsString(tempItemData);
                LoadControlHint(newButton.isInventoryIcon);
            }
        } else {
            itemInfo.Visible = false;
            tempItemData = new Dictionary();
        }
    }

    //грузим подсказки по управлению предметом
    protected virtual void LoadControlHint(bool isInventoryIcon)
    {
        controlHints.Text = InterfaceLang.GetPhrase(
            "inventory", 
            "inventoryControlHints", 
            tempItemData["type"].ToString()
        );
    }

    protected ItemIcon FirstEmptyButton {
        get {
            foreach(ItemIcon button in itemButtons) {
                if (button.myItemCode == null) {
                    return button;
                }
            }
            return null;
        }
    }

    private ItemIcon AddNewItem(string itemCode) {
        ItemIcon emptyButton = FirstEmptyButton;
        if (emptyButton != null) {
            emptyButton.SetItem(itemCode);

            if (itemCode.Contains("key")) {
                inventory.AddKey(itemCode);
            }
        } else {
            inventory.ItemsMessage("space");
        }
        
        return emptyButton;
    }
   
    protected void RemoveTempItem()
    {
        if (tempButton.myItemCode.Contains("key")) {
            PlayerInventory inventory = Global.Get().player.inventory;
            inventory.RemoveKey(tempButton.myItemCode);
        }
        if (tempButton.GetBindKey() != "") {
            int keyId = int.Parse(tempButton.GetBindKey());
            menu.bindedButtons.Remove(keyId);
            bindsList.RemoveIcon(tempButton);
        }

        tempButton.ClearItem();
        tempButton = null;
    }

    protected virtual string GetItemPropsString(Dictionary itemProps)
    {
        string result = "";
        Dictionary itemPropNames = InterfaceLang.GetPhrasesSection("inventory", "itemProps");
        foreach(string prop in itemProps.Keys) {
            if (itemPropNames.Contains(prop)) {
                string propName = itemPropNames[prop].ToString();
                string propValue = itemProps[prop].ToString();
                if (prop == "medsEffect") {
                    propValue = InterfaceLang.GetPhrase("inventory", "medsEffects", propValue);
                }
                if (prop == "damageBlock") {
                    //выводим блокирование урона в процентах
                    float blockValue = Global.ParseFloat(propValue);
                    propValue = (blockValue * 100f).ToString() + "%";
                }
                if (prop == "questItem")
                {
                    propValue = "";
                }
                
                result += "> " + propName + propValue + "\n";
            }
        }
        return result;
    }

    protected void CheckTempIcon()
    {
        if (tempButton != null) {
            tempButton._on_itemIcon_mouse_exited();
            tempButton = null;
        }
    }

    protected bool checkMouseInButton(Control button) 
    {
        var mouse = button.GetLocalMousePosition();
        return mouse.x >= 0 && mouse.x <= button.RectSize.x 
            && mouse.y >= 0 && mouse.y <= button.RectSize.y;
    }

    protected virtual bool IconsInSameArray(ItemIcon oldButton, ItemIcon newButton) 
    {
        return itemButtons.Contains(oldButton) && itemButtons.Contains(newButton);
    }

    protected virtual void ChangeItemButtons(ItemIcon oldButton, ItemIcon newButton)
    {
        //меняем местами бинды клавиш на кнопках
        string tempBind = oldButton.GetBindKey();
        oldButton.SetBindKey(newButton.GetBindKey());
        newButton.SetBindKey(tempBind);

        if (oldButton.GetBindKey() != "") {
            int keyId = int.Parse(oldButton.GetBindKey());
            menu.bindedButtons[keyId] = oldButton;
        }
        if (newButton.GetBindKey() != "") {
            int keyId = int.Parse(newButton.GetBindKey());
            menu.bindedButtons[keyId] = newButton;
        }

        //меняем местами количество предметов (если это патроны)
        int tempCount = oldButton.GetCount();
        oldButton.SetCount(newButton.GetCount(), false);
        newButton.SetCount(tempCount, false);
        
        //меняем местами вещи на кнопках
        if (newButton.myItemCode == null) {
            var tempItemCode = oldButton.myItemCode;
            oldButton.ClearItem();
            newButton.SetItem(tempItemCode);
        } else {
            var tempItemCode = oldButton.myItemCode;
            oldButton.SetItem(newButton.myItemCode);
            newButton.SetItem(tempItemCode);
        }
    }

    protected virtual void CheckDragItem() {}

    private void LoadLabels()
    {
        foreach(string labelName in labels.Keys) {
            Label tempLabel = labels[labelName];
            tempLabel.Text = InterfaceLang.GetPhrase("inventory", "labels", labelName);
        }
    }
    
    public virtual async void OpenMenu() 
    {
        player.MayMove = false;

        LoadLabels();
        moneyCount.Text = inventory.money.ToString();

        menu.Visible = true;
        
        if (!isAnimating) {
            isAnimating = true;
            while (back.RectPosition.x > -MENU_SIZE) {
                Vector2 newPos = back.RectPosition;
                newPos.x -= MENU_SPEED;
                back.RectPosition = newPos;
                await player.ToSignal(player.GetTree(), "idle_frame");
            }

            Input.SetMouseMode(Input.MouseMode.Visible);
            isAnimating = false;
        }

        menu.isOpen = true;
    }

    public virtual async void CloseMenu()
    {
        CheckTempIcon();
        player.MayMove = true;

        isAnimating = true;
        while (back.RectPosition.x < 0) {
            Vector2 newPos = back.RectPosition;
            newPos.x += MENU_SPEED;
            back.RectPosition = newPos;
            await player.ToSignal(player.GetTree(), "idle_frame");
        }
        menu.Visible = false;
        menu.isOpen = false;
        if (!Global.Get().paused) {
            Input.SetMouseMode(Input.MouseMode.Captured);
        }

        isAnimating = false;
    }

    protected bool UpdateDragging(InputEvent @event)
    {
         if (Input.IsActionPressed("ui_click") && @event is InputEventMouseMotion) {
                if (!isDragging) {
                    dragIcon.Texture = tempButton.GetIcon();
                    tempButton.SetIcon(null);
                    isDragging = true;
                }
                if (dragTimer < 1) {
                    dragTimer += 0.5f;
                }
                dragIcon.RectGlobalPosition = menu.GetGlobalMousePosition();
            }
            if (Input.IsActionJustReleased("ui_click")) {
                if (isDragging) {
                    tempButton.SetIcon((StreamTexture)dragIcon.Texture);
                    dragIcon.Texture = null;
                    dragIcon.RectGlobalPosition = Vector2.Zero;
                    isDragging = false;
                }

                if (dragTimer >= 1) {
                    dragTimer = 0;
                    CheckDragItem();
                    return true;
                }
            }
        return false;
    }

    public virtual void CloseModal() {}
    public virtual void UpdateInput(InputEvent @event) {}

    public virtual void _on_modal_no_pressed() {}
    public virtual void _on_modal_yes_pressed() {}
    public virtual void _on_count_value_changed(float newCount) { }
    public virtual void _on_takeAll_pressed() {}
}