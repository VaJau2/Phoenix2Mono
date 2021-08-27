using Godot.Collections;

namespace DialogueScripts
{
    //получить предмет во время диалога 
    //(если нет места, предмет положится в сумку
    public class TakeItem: IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(parameter)) return;
            Dictionary itemData = ItemJSON.GetItemData(parameter);
            if (itemData.Count == 0) return;
            
            InventoryMenu inventory = dialogueMenu.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            Messages messages = dialogueMenu.GetNode<Messages>("/root/Main/Scene/canvas/messages");
            messages.ShowMessage("itemTaken", itemData["name"].ToString(), "items");
            if (!inventory.AddOrDropItem(parameter))
            {
                messages.ShowMessage("space", "items", 2.5f);
            }
        }

    }
}