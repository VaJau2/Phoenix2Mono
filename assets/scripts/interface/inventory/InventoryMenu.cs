using Godot;
using Godot.Collections;

public class InventoryMenu : Control
{
    private const float MENU_SPEED = 16f;
    private const float MENU_SIZE = 272f;
    private Control back;
    private Control wearBack;

    private Control itemInfo;
    private Label itemName;
    private Label itemDesc;
    private Label itemProps;
    private Label controlHints;

    private bool isOpen = false;
    private bool isAnimating = false;
    public bool isDragging {get; private set;} = false;

    private ItemIcon tempButton;
    private Dictionary tempItemData;
    private Array<ItemIcon> itemButtons = new Array<ItemIcon>();
    private ItemIcon weaponButton;
    private ItemIcon armorButton;
    private ItemIcon artifactButton;
    private Dictionary<int, ItemIcon> bindedButtons = new Dictionary<int, ItemIcon>();
    //key = key, value = button
    private Dictionary<string, Label> labels = new Dictionary<string, Label>();

    
    private float dragTimer = 0;
    private TextureRect dragIcon;
    private PlayerInventory inventory => Global.Get().player.inventory;

    public ItemIcon FirstEmptyButton {
        get {
            foreach(ItemIcon button in itemButtons) {
                if (button.myItemCode == null) {
                    return button;
                }
            }
            return null;
        }
    }

    public ItemIcon AddNewItem(string itemCode) {
        ItemIcon emptyButton = FirstEmptyButton;
        if (emptyButton != null) {
            emptyButton.SetItem(itemCode);

            if (itemCode.Contains("key")) {
                PlayerInventory inventory = Global.Get().player.inventory;
                inventory.AddKey(itemCode);
            }
        } else {
            inventory.MessageNotEnoughSpace();
        }
        
        return emptyButton;
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

    private bool ItemIsBindable(string itemType) 
    {
        return itemType == "weapon" || itemType == "food" || itemType == "meds";
    }

    private void BindHotkeys() 
    {
        if (tempButton.myItemCode != null) {
            if (ItemIsBindable(tempItemData["type"].ToString())) {
                for (int i = 0; i < 10; i++) {
                    if (Input.IsKeyPressed(48 + i)) {
                        //если клавиша уже забиндена
                        if (bindedButtons.Keys.Contains(i)) {
                            //если нажата та же кнопка, она стирается
                            if (bindedButtons[i] == tempButton) {
                                tempButton.SetBindKey(null);
                                bindedButtons.Remove(i);
                            } else {
                            //если на ту же кнопку биндится другая кнопка, предыдущая стирается
                                ItemIcon oldBindedButton = bindedButtons[i];
                                oldBindedButton.SetBindKey(null);
                                bindedButtons[i] = tempButton;
                                tempButton.SetBindKey(i.ToString());
                            }
                        } else {
                            //если кнопка биндится впервые
                            bindedButtons[i] = tempButton;
                            tempButton.SetBindKey(i.ToString());
                        }
                    }
                }
            }
        }
    }

    private void UseHotkeys() 
    {
        for (int i = 0; i < 10; i++) {
            if (Input.IsKeyPressed(48 + i) && bindedButtons.Keys.Contains(i)) {
                SetTempButton(bindedButtons[i], false);
                UseTempItem();
            }
        }
    }

    private bool canTakeItemOff() 
    {
        string itemType = tempItemData["type"].ToString();
        if (itemType == "artifact" && inventory.artifact != "") {
            Dictionary artifactData = ItemJSON.GetItemData(inventory.artifact);
            if (artifactData.Contains("cantUnwear")) {
                inventory.MessageCantUnwear(artifactData["name"].ToString());
                return false;
            }
        }
        
        return true;
    }

    private void WearTempItem(ItemIcon wearButton)
    {
        //если вещь надевается
        if (tempButton != wearButton) {
            //если уже надета другая вещь
            if (wearButton.myItemCode != null) {
                if (!canTakeItemOff()) return;
                inventory.UnwearItem(wearButton.myItemCode, false);
            }
            ChangeItemButtons(tempButton, wearButton);
            inventory.WearItem(wearButton.myItemCode);
        } //если вещь снимается 
        else {
            if (!canTakeItemOff()) return;

            ItemIcon otherButton = FirstEmptyButton;
            //если в инвентаре есть место
            if (otherButton != null) {
                inventory.UnwearItem(wearButton.myItemCode);
                ChangeItemButtons(wearButton, otherButton);
            } else {
                DropTempItem();
            }
        }
    }

    private void UseTempItem()
    {
        string itemType = tempItemData["type"].ToString();
        if (inventory.itemIsUsable(itemType)) {
            switch(itemType) {
                case "weapon":
                    WearTempItem(weaponButton);
                    break;
                case "armor":
                    WearTempItem(armorButton);
                    break;
                case "artifact":
                    WearTempItem(artifactButton);
                    break;
                default:
                    inventory.UseItem(tempItemData);
                    RemoveTempItem();
                    break;
            }
        }
        tempButton = null;
    }

    private void DropTempItem() 
    {
        RemoveTempItem();
    }

    private void RemoveTempItem()
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

    private string GetItemPropsString(Dictionary itemProps)
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

    private void CheckTempIcon()
    {
        if (tempButton != null) {
            tempButton._on_itemIcon_mouse_exited();
            tempButton = null;
        }
    }

    private bool checkMouseInButton(Control button) 
    {
        var mouse = button.GetLocalMousePosition();
        return mouse.x >= 0 && mouse.x <= button.RectSize.x 
            && mouse.y >= 0 && mouse.y <= button.RectSize.y;
    }

    private void ChangeItemButtons(ItemIcon oldButton, ItemIcon newButton)
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
        oldButton.SetCount(newButton.GetCount());
        newButton.SetCount(tempCount);
        
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

    private bool isUnwearingItem(string itemType)
    {
        return (itemType == "weapon" && tempButton == weaponButton)
        || (itemType == "armor" && tempButton == armorButton)
        || (itemType == "artifact" && tempButton == artifactButton);
    }

    private void CheckDragItem() 
    {
        string itemType = tempItemData["type"].ToString();

        if (itemType == "weapon" && checkMouseInButton(weaponButton)) {
            WearTempItem(weaponButton); return;
        }
        if (itemType == "armor" && checkMouseInButton(armorButton)) {
            WearTempItem(armorButton); return;
        }
        if (itemType == "artifact" && checkMouseInButton(artifactButton)) {
            WearTempItem(artifactButton); return;
        }
        foreach(ItemIcon otherButton in itemButtons) {
            Control buttonControl = otherButton as Control;
            if(tempButton != otherButton && checkMouseInButton(buttonControl)) {
                if(isUnwearingItem(itemType)) {
                    if (!canTakeItemOff()) return;
                    inventory.UnwearItem(tempButton.myItemCode);
                }

                ChangeItemButtons(tempButton, otherButton);
                SetTempButton(otherButton, false);
                dragIcon.Texture = null;
            }
        }
    }

    private void LoadLabels()
    {
        foreach(string labelName in labels.Keys) {
            Label tempLabel = labels[labelName];
            tempLabel.Text = InterfaceLang.GetPhrase("inventory", "labels", labelName);
        }
    }
    
    private async void OpenMenu(bool showWear = true) 
    {
        LoadLabels();

        back.Visible = true;
        isAnimating = true;
        float startPos = back.RectPosition.x;
        while (back.RectPosition.x > startPos - MENU_SIZE) {
            Vector2 newPos = back.RectPosition;
            newPos.x -= MENU_SPEED;
            back.RectPosition = newPos;
            await ToSignal(GetTree(), "idle_frame");
        }
        if (showWear) {
            wearBack.Visible = true;
        }
        Input.SetMouseMode(Input.MouseMode.Visible);
        isOpen = true;
        isAnimating = false;
    }

    private async void CloseMenu()
    {
        CheckTempIcon();

        isAnimating = true;
        wearBack.Visible = false;
        float startPos = back.RectPosition.x;
        while (back.RectPosition.x < startPos + MENU_SIZE) {
            Vector2 newPos = back.RectPosition;
            newPos.x += MENU_SPEED;
            back.RectPosition = newPos;
            await ToSignal(GetTree(), "idle_frame");
        }
        back.Visible = false;
        Input.SetMouseMode(Input.MouseMode.Captured);

        isOpen = false;
        isAnimating = false;
    }

    public override void _Ready() 
    {
        foreach(object button in GetNode<Control>("back/items").GetChildren()) {
            itemButtons.Add(button as ItemIcon); 
        }
        back     = GetNode<Control>("back");
        wearBack = GetNode<Control>("back/wearBack");
        weaponButton    = wearBack.GetNode<ItemIcon>("weapon");
        armorButton     = wearBack.GetNode<ItemIcon>("armor");
        artifactButton  = wearBack.GetNode<ItemIcon>("artifact");

        itemInfo = GetNode<Control>("back/itemInfo");
        itemName = itemInfo.GetNode<Label>("name");
        itemDesc = itemInfo.GetNode<Label>("description");
        itemProps = itemInfo.GetNode<Label>("props");
        controlHints = itemInfo.GetNode<Label>("hints");
        dragIcon = GetNode<TextureRect>("back/dragIcon");

        labels.Add("name", GetNode<Label>("back/Label"));
        labels.Add("money", GetNode<Label>("back/moneyLabel"));
        labels.Add("wear", GetNode<Label>("back/wearBack/Label"));
        labels.Add("weapon", GetNode<Label>("back/wearBack/weaponLabel"));
        labels.Add("armor", GetNode<Label>("back/wearBack/armorLabel"));
        labels.Add("artifact", GetNode<Label>("back/wearBack/artifactLabel"));
    }

    public override void _Input(InputEvent @event)
    {
        if (!isAnimating && @event is InputEventKey) {
            if (Input.IsActionJustPressed("ui_focus_next")) {
                if (isOpen) {
                    CloseMenu();
                } 
                else {
                    OpenMenu();
                }
            }
        }

        if (isOpen && tempButton != null) {
            if (Input.IsActionPressed("ui_click") && @event is InputEventMouseMotion) {
                if (!isDragging) {
                    dragIcon.Texture = tempButton.GetIcon();
                    tempButton.SetIcon(null);
                    isDragging = true;
                }
                if (dragTimer < 1) {
                    dragTimer += 0.5f;
                }
                dragIcon.RectGlobalPosition = GetGlobalMousePosition();
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
                    return;
                }

                UseTempItem();
            }

            if (Input.IsMouseButtonPressed(2) && !isDragging && tempButton != null) {
                DropTempItem();
            }

            if (@event is InputEventKey) {
                BindHotkeys();
            }
        }
        if(@event is InputEventKey && tempButton == null) {
            UseHotkeys();
        }
    }
}
