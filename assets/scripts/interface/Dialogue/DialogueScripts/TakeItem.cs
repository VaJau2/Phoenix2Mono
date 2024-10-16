using Godot;

namespace DialogueScripts;

//получить предмет во время диалога 
//(если нет места, предмет положится в сумку
public class TakeItem: IDialogueScript
{
    public void initiate(Node node, string parameter, string key = "")
    {
        if (string.IsNullOrEmpty(parameter)) return;
        
        var itemData = ItemJSON.GetItemData(parameter);
        
        if (itemData.Count == 0) return;
            
        var inventory = node.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        var messages = node.GetNode<Messages>("/root/Main/Scene/canvas/messages");
        messages.ShowMessage("itemTaken", itemData["name"].ToString(), "items");
        
        if (!inventory.AddOrDropItem(parameter))
        {
            messages.ShowMessage("space", "items", 2.5f);
        }
    }

}