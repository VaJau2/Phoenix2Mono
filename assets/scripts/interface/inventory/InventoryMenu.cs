using Godot;
using Godot.Collections;

public class InventoryMenu : Control
{
    private const float MENU_SPEED = 16f;
    private const float MENU_SIZE = 272f;
    private Global global;
    private Control back;
    private Control wearBack;

    private Control itemInfo;
    private Label itemName;
    private Label itemDesc;
    private Label itemPrice;
    private Label controlHints;

    private bool isOpen = false;
    private bool isAnimating = false;

    private ItemIcon tempIcon;
    private Array itemIcons = new Array();

    private Dictionary itemsData = new Dictionary();


    private Dictionary<string, Label> labels = new Dictionary<string, Label>();


    public void SetTempIcon(ItemIcon newIcon)
    {
        tempIcon = newIcon;
        if (newIcon != null) {
            itemInfo.Visible = true;
            Dictionary itemData = ItemJSON.GetItemData(newIcon.myItemCode);
            itemName.Text = itemData["name"].ToString();
            itemDesc.Text = itemData["description"].ToString();
            itemPrice.Text = itemData["price"].ToString();
            controlHints.Text = InterfaceLang.GetPhrase(
                "inventory", 
                "inventoryControlHints", 
                itemData["type"].ToString()
            );
        } else {
            itemInfo.Visible = false;
        }
    }

    private void UpdateItemIcons()
    {
        Player player = global.player;

        for (int i = 0; i < itemIcons.Count; i++) {
            ItemIcon temp = itemIcons[i] as ItemIcon;
            if (player.inventory.Items.Count > i) {
                temp.SetItem(player.inventory.Items[i]);
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
        global = Global.Get();

        back     = GetNode<Control>("back");
        wearBack = GetNode<Control>("back/wearBack");
        itemIcons = GetNode<Control>("back/items").GetChildren(); 

        itemInfo = GetNode<Control>("back/itemInfo");
        itemName = itemInfo.GetNode<Label>("name");
        itemDesc = itemInfo.GetNode<Label>("description");
        itemPrice = itemInfo.GetNode<Label>("price");
        controlHints = itemInfo.GetNode<Label>("hints");

        labels.Add("name", GetNode<Label>("back/Label"));
        labels.Add("money", GetNode<Label>("back/moneyLabel"));
        labels.Add("wear", GetNode<Label>("back/wearBack/Label"));
        labels.Add("price", GetNode<Label>("back/itemInfo/priceLabel"));
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
    }
}

public static class ItemJSON
{
    static string tempLang;
    static Dictionary itemsData = new Dictionary();
    public static Dictionary GetItemData(string itemCode)
    {
        string lang = InterfaceLang.GetLang();

        if (tempLang != lang) {
            string path = "assets/lang/" + lang + "/items.json";
            itemsData = Global.loadJsonFile(path);
            tempLang = lang;
        }
        
        return (Dictionary)itemsData[itemCode];
    }
}