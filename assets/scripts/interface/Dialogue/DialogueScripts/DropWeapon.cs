using Godot;

namespace DialogueScripts;

public class DropWeapon : IDialogueScript
{
    public void initiate(Node node, string parameter, string key = "")
    {
        var player = Global.Get().player;
        var weapon = player.Inventory.weapon;
        var inventory = node.GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
        var weaponButton = inventory.GetNode<ItemIcon>("helper/back/wearBack/weapon");

        if (string.IsNullOrEmpty(weapon)) return;
        
        player.Inventory.UnwearItem(weapon);
        inventory.mode.RemoveItemFromButton(weaponButton);
        var tempBag = inventory.mode.bagSpawner.SpawnItemBag();
        tempBag.ChestHandler.ItemCodes.Add(weapon);
    }
}