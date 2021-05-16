using Godot.Collections;

namespace DialogueScripts
{
    public class GiveItem : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter)
        {
            if (string.IsNullOrEmpty(parameter)) return;
            InventoryMenu inventory = dialogueMenu.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            inventory.RemoveItemIfExists(parameter);
            
            Dictionary itemData = ItemJSON.GetItemData(parameter);
            Messages messages = dialogueMenu.GetNode<Messages>("/root/Main/Scene/canvas/messages");
            messages.ShowMessage("itemGiven", itemData["name"].ToString(), "items");
        }
    }
}