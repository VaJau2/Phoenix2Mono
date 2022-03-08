using Godot.Collections;

namespace DialogueScripts
{
    class WearItem : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            if (string.IsNullOrEmpty(parameter)) return;
            Dictionary itemData = ItemJSON.GetItemData(parameter);
            if (itemData.Count == 0) return;

            string itemType = itemData["type"].ToString();
            if (itemType != "weapon" && itemType != "armor" && itemType != "artifact") return;

            PlayerInventory inventory = Global.Get().player.inventory;
            inventory.WearItem(parameter);
            var wearButton = inventory.GetWearButton(itemType);
            wearButton.SetItem(parameter);
        }
    }
}