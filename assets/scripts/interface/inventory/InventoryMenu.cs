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

    private ItemIcon tempIcon;
    private Dictionary tempItemData;
    private Array itemIcons = new Array();

    private Dictionary<string, Label> labels = new Dictionary<string, Label>();

    public void SetTempIcon(ItemIcon newIcon)
    {
        tempIcon = newIcon;
        if (newIcon != null) {
            itemInfo.Visible = true;
            tempItemData = ItemJSON.GetItemData(newIcon.myItemCode);
            itemName.Text = tempItemData["name"].ToString();
            itemDesc.Text = tempItemData["description"].ToString();
            itemProps.Text = GetItemPropsString(tempItemData);
            controlHints.Text = InterfaceLang.GetPhrase(
                "inventory", 
                "inventoryControlHints", 
                tempItemData["type"].ToString()
            );
        } else {
            itemInfo.Visible = false;
            tempItemData = new Dictionary();
        }
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

    private void UpdateItemIcons()
    {
        PlayerInventory inventory = Global.Get().player.inventory;

        for (int i = 0; i < itemIcons.Count; i++) {
            ItemIcon temp = itemIcons[i] as ItemIcon;
            if (inventory.Items.Count > i) {
                temp.SetItem(inventory.Items[i], i);
            } else {
                temp.ClearItem();
            }
        }
    }

    private void CheckTempIcon()
    {
        if (tempIcon != null) {
            tempIcon._on_itemIcon_mouse_exited();
            tempIcon = null;
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
        UpdateItemIcons();

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
        back     = GetNode<Control>("back");
        wearBack = GetNode<Control>("back/wearBack");
        itemIcons = GetNode<Control>("back/items").GetChildren(); 

        itemInfo = GetNode<Control>("back/itemInfo");
        itemName = itemInfo.GetNode<Label>("name");
        itemDesc = itemInfo.GetNode<Label>("description");
        itemProps = itemInfo.GetNode<Label>("props");
        controlHints = itemInfo.GetNode<Label>("hints");

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

        if (isOpen && @event is InputEventMouse && tempIcon != null) {
            PlayerInventory inventory = Global.Get().player.inventory;

            if (Input.IsMouseButtonPressed(1)) {
                if (inventory.itemIsUsable(tempItemData["type"].ToString())) {
                    inventory.UseItem(tempIcon.myItemNumber, tempItemData);
                    UpdateItemIcons();
                }
            } else if(Input.IsMouseButtonPressed(2)) {
                inventory.DropItem(tempIcon.myItemNumber);
            }
        }
    }
}
