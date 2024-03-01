using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class LoadItemsFromSuitcase : TriggerBase
{
    [Export] private NodePath suitcasePath;
    
    private SaveNode saveNode;

    public override async void _Ready()
    {
        saveNode = GetNodeOrNull<SaveNode>("/root/Main/SaveNode");
        if (!IsActive) return;
        
        await Global.Get().ToTimer(0.1f, this);
        
        OnActivateTrigger();
    }

    public override void OnActivateTrigger()
    {
        FurnChest suitcase = GetNode<FurnChest>(suitcasePath);
        if (suitcase == null)
        {
            base.OnActivateTrigger();
            return;
        }
        
        bool suitcaseEmpty = true;
        bool playerHasSuitcase = Global.Get().player.Inventory.HasItem("Quest_suitcaseVacation");
        
        if (IsInstanceValid(saveNode) && playerHasSuitcase)
        {
            InventoryMenu menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            menu.RemoveItemIfExists("Quest_suitcaseVacation");
            
            Dictionary data = saveNode.InventoryData;
            
            
            if (data.TryGetValue("weapon", out var weaponValue))
            {
                suitcase.ChestHandler.AddNewItem(weaponValue.ToString());
                suitcaseEmpty = false;
            }
            if (data.ContainsKey("cloth") && data["cloth"].ToString() != "empty")
            {
                suitcase.ChestHandler.AddNewItem(data["cloth"].ToString());
                suitcaseEmpty = false;
            }
            if (data.TryGetValue("artifact", out var artifactValue))
            {
                suitcase.ChestHandler.AddNewItem(artifactValue.ToString());
                suitcaseEmpty = false;
            }
            
            if (suitcaseEmpty)
            {
                Global.AddDeletedObject(suitcase.Name);
                suitcase.QueueFree();
                
                base.OnActivateTrigger();
                return;
            }
            
            Array itemCodes = (Array) data["itemCodes"];
            Array itemCounts = (Array) data["itemCounts"];

            for (int i = 0; i < itemCodes.Count; i++)
            {
                var itemCode = itemCodes[i].ToString();
                if (itemCode == "_") continue;
                
                var itemCount = Convert.ToInt32(itemCounts[i]);
                if (itemCount > 0)
                {
                    suitcase.ChestHandler.AmmoCount.Add(itemCode, itemCount);
                }
                else
                {
                    suitcase.ChestHandler.AddNewItem(itemCode);
                }
            }
        }
        else
        {
            Global.AddDeletedObject(suitcase.Name);
            suitcase.QueueFree();
        }

        base.OnActivateTrigger();
    }
}
