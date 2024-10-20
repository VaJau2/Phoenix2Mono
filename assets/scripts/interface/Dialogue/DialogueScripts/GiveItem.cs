using Godot;

namespace DialogueScripts;

public class GiveItem : IDialogueScript
{
    public void initiate(Node node, string parameter, string key = "")
    {
        if (string.IsNullOrEmpty(parameter)) return;
        
        var itemData = ItemJSON.GetItemData(parameter);
        
        if (itemData.Count == 0) return;
            
        var inventory = node.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        inventory.RemoveItemIfExists(parameter);
            
        var  messages = node.GetNode<Messages>("/root/Main/Scene/canvas/messages");
        messages.ShowMessage("itemGiven", itemData["name"].ToString(), "items");
    }
}