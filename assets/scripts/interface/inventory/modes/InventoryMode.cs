using Godot;
using Godot.Collections;

public abstract class InventoryMode
{
    protected const float MENU_SPEED = 16f;
    protected const float MENU_SIZE = 272f;

    protected Player player;
    protected InventoryMenu menu;
    protected Control back;

    protected Control itemInfo;
    protected Label itemName;
    protected Label itemDesc;
    protected Label itemProps;
    protected Label controlHints;

    protected bool isAnimating = false;
    public bool isDragging {get; protected set;} = false;

    protected ItemIcon tempButton;
    protected Dictionary tempItemData;
    protected Array<ItemIcon> itemButtons = new Array<ItemIcon>();
    protected Dictionary<int, ItemIcon> bindedButtons = new Dictionary<int, ItemIcon>();
    //key = key, value = button
    protected Dictionary<string, Label> labels = new Dictionary<string, Label>();

    protected float dragTimer = 0;
    protected TextureRect dragIcon;
    protected PlayerInventory inventory => player.inventory;

    public InventoryMode(InventoryMenu menu)
    {
        this.menu = menu;

        foreach(object button in menu.GetNode<Control>("back/items").GetChildren()) {
            itemButtons.Add(button as ItemIcon); 
        }
        back = menu.GetNode<Control>("back");

        itemInfo = menu.GetNode<Control>("back/itemInfo");
        itemName = itemInfo.GetNode<Label>("name");
        itemDesc = itemInfo.GetNode<Label>("description");
        itemProps = itemInfo.GetNode<Label>("props");
        controlHints = itemInfo.GetNode<Label>("hints");
        dragIcon = menu.GetNode<TextureRect>("back/dragIcon");

        labels.Add("name", menu.GetNode<Label>("back/Label"));
        labels.Add("money", menu.GetNode<Label>("back/moneyLabel"));
        labels.Add("wear", menu.GetNode<Label>("back/wearBack/Label"));
        labels.Add("weapon", menu.GetNode<Label>("back/wearBack/weaponLabel"));
        labels.Add("armor", menu.GetNode<Label>("back/wearBack/armorLabel"));
        labels.Add("artifact", menu.GetNode<Label>("back/wearBack/artifactLabel"));
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
                controlHints.Text = InterfaceLang.GetPhrase(
                    "inventory", 
                    "inventoryControlHints", 
                    tempItemData["type"].ToString()
                );
            }
        } else {
            itemInfo.Visible = false;
            tempItemData = new Dictionary();
        }
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
            inventory.MessageNotEnoughSpace();
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
            bindedButtons.Remove(keyId);
        }

        tempButton.ClearItem();
        tempButton = null;
    }

    protected string GetItemPropsString(Dictionary itemProps)
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

    protected void ChangeItemButtons(ItemIcon oldButton, ItemIcon newButton)
    {
        //меняем местами бинды клавиш на кнопках
        string tempBind = oldButton.GetBindKey();
        oldButton.SetBindKey(newButton.GetBindKey());
        newButton.SetBindKey(tempBind);

        if (oldButton.GetBindKey() != "") {
            int keyId = int.Parse(oldButton.GetBindKey());
            bindedButtons[keyId] = oldButton;
        }
        if (newButton.GetBindKey() != "") {
            int keyId = int.Parse(newButton.GetBindKey());
            bindedButtons[keyId] = newButton;
        }

        //меняем местами количество предметов (если это патроны)
        int tempCount = oldButton.GetCount();
        oldButton.SetCount(newButton.GetCount(), false);
        newButton.SetCount(tempCount, false);
        
        //меняем местами вещи на кнопках
        if (newButton.myItemCode == null) {
            newButton.SetItem(oldButton.myItemCode);
            oldButton.ClearItem();
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
        if (player == null) {
            player = Global.Get().player;
        }
        player.MayMove = false;

        LoadLabels();

        back.Visible = true;
        isAnimating = true;
        float startPos = back.RectPosition.x;
        while (back.RectPosition.x > startPos - MENU_SIZE) {
            Vector2 newPos = back.RectPosition;
            newPos.x -= MENU_SPEED;
            back.RectPosition = newPos;
            await player.ToSignal(player.GetTree(), "idle_frame");
        }

        Input.SetMouseMode(Input.MouseMode.Visible);
        menu.isOpen = true;
        isAnimating = false;
    }

    public virtual async void CloseMenu()
    {
        CheckTempIcon();
        player.MayMove = true;

        isAnimating = true;
        float startPos = back.RectPosition.x;
        while (back.RectPosition.x < startPos + MENU_SIZE) {
            Vector2 newPos = back.RectPosition;
            newPos.x += MENU_SPEED;
            back.RectPosition = newPos;
            await player.ToSignal(player.GetTree(), "idle_frame");
        }
        back.Visible = false;
        menu.isOpen = false;
        Input.SetMouseMode(Input.MouseMode.Captured);

        isAnimating = false;
    }

    protected virtual void CloseWithoutAnimating()
    {
        CheckTempIcon();
        player.MayMove = true;
        Vector2 newPos = back.RectPosition;
        newPos.x += MENU_SIZE;
        back.RectPosition = newPos;
        back.Visible = false;
        menu.isOpen = false;
        Input.SetMouseMode(Input.MouseMode.Captured);
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

    public void UpdateOpen(InputEvent @event)
    {
        if (!isAnimating && @event is InputEventKey) {
            if (Input.IsActionJustPressed("inventory")) {
                if (menu.isOpen) {
                    CloseMenu();
                } 
                else {
                    OpenMenu();
                }
            }
            if (Input.IsActionJustPressed("ui_cancel") && menu.isOpen) {
                CloseWithoutAnimating();
            }
        }
    }

    public virtual void UpdateInput(InputEvent @event) {}
}