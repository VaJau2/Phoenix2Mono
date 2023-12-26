namespace DialogueScripts
{
    public class DropWeapon : IDialogueScript
    {
        public void initiate(DialogueMenu dialogueMenu, string parameter, string key = "")
        {
            Player player = Global.Get().player;
            string weapon = player.Inventory.weapon;
            InventoryMenu inventory = dialogueMenu.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            ItemIcon weaponButton = inventory.GetNode<ItemIcon>("helper/back/wearBack/weapon");
            
            if (!string.IsNullOrEmpty(weapon))
            {
                player.Inventory.UnwearItem(weapon);
                inventory.mode.RemoveItemFromButton(weaponButton);
                var tempBag = inventory.mode.SpawnItemBag();
                tempBag.ChestHandler.ItemCodes.Add(weapon);
            }
        }
    }
}