using Godot;
using Godot.Collections;

namespace DialogueScripts
{
    public class ChangeItemName : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(parameter) || string.IsNullOrEmpty(key)) return;
            Dictionary itemData = ItemJSON.GetItemData(parameter);
            if (itemData.Count == 0) return;
            
            InventoryMenu inventory = dialogueMenu.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            inventory.RemoveItemIfExists(key);
            
            Messages messages = dialogueMenu.GetNode<Messages>("/root/Main/Scene/canvas/messages");
            if (!inventory.AddOrDropItem(parameter))
            {
                messages.ShowMessage("space", "items", 2.5f);
            }
        }
    }
}