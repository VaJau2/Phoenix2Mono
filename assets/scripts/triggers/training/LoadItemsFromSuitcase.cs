using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class LoadItemsFromSuitcase : TriggerBase
{
    [Export] private NodePath suitcasePath;
    
    private SaveNode saveNode;

    public override async void _Ready()
    {
        saveNode = GetNodeOrNull<SaveNode>("/root/Main/SaveNode");
        if (!IsActive) return;
        
        await Global.Get().ToTimer(0.1f, this);
        
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        FurnChest suitcase = GetNode<FurnChest>(suitcasePath);
        if (suitcase == null)
        {
            base._on_activate_trigger();
            return;
        }
        
        bool suitcaseEmpty = true;
        bool playerHasSuitcase = Global.Get().player.inventory.HasItem("suitcase_vacation");
        
        if (saveNode != null && playerHasSuitcase)
        {
            InventoryMenu menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            menu.RemoveItemIfExists("suitcase_vacation");
            
            Dictionary data = saveNode.InventoryData;
            
            
            if (!string.IsNullOrEmpty(data["weapon"].ToString()))
            {
                suitcase.AddNewItem(data["weapon"].ToString());
                suitcaseEmpty = false;
            }
            if (!string.IsNullOrEmpty(data["cloth"].ToString()))
            {
                suitcase.AddNewItem(data["cloth"].ToString());
                suitcaseEmpty = false;
            }
            if (!string.IsNullOrEmpty(data["artifact"].ToString()))
            {
                suitcase.AddNewItem(data["artifact"].ToString());
                suitcaseEmpty = false;
            }
            
            Array itemCodes = (Array) data["itemCodes"];
            Array itemCounts = (Array) data["itemCounts"];

            for (int i = 0; i < itemCodes.Count; i++)
            {
                var itemCode = itemCodes[i].ToString();
                if (itemCode == "_") continue;

                var itemCount = Convert.ToInt32(itemCounts[i]);
                if (itemCount > 1)
                {
                    suitcase.ammoCount.Add(itemCode, itemCount);
                }
                else
                {
                    suitcase.AddNewItem(itemCode);
                }
            }
        }

        if (suitcaseEmpty)
        {
            suitcase.QueueFree();
        }
        
        base._on_activate_trigger();
    }
}