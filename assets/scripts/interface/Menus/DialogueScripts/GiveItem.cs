﻿using Godot.Collections;

namespace DialogueScripts
{
    public class GiveItem : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter)
        {
            if (string.IsNullOrEmpty(parameter)) return;
            Dictionary itemData = ItemJSON.GetItemData(parameter);
            if (itemData.Count == 0) return;
            
            InventoryMenu inventory = dialogueMenu.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            inventory.RemoveItemIfExists(parameter);
            
            Messages messages = dialogueMenu.GetNode<Messages>("/root/Main/Scene/canvas/messages");
            messages.ShowMessage("itemGiven", itemData["name"].ToString(), "items");
        }
    }
}