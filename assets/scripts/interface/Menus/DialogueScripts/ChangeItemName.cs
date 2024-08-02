using Godot;

namespace DialogueScripts;

public class ChangeItemName : IDialogueScript
{
    public void initiate(Node node, string parameter, string key = "")
    {
        if (string.IsNullOrEmpty(parameter) || string.IsNullOrEmpty(key)) return;
        
        var itemData = ItemJSON.GetItemData(parameter);
        
        if (itemData.Count == 0) return;
            
        var inventory = node.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.RemoveItemIfExists(key);
            
        var messages = node.GetNode<Messages>("/root/Main/Scene/canvas/messages");
        
        if (!inventory.AddOrDropItem(parameter))
        {
            messages.ShowMessage("space", "items", 2.5f);
        }
    }
}