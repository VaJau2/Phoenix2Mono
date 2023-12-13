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
        bool playerHasSuitcase = Global.Get().player.inventory.HasItem("Quest_suitcaseVacation");
        
        if (IsInstanceValid(saveNode) && playerHasSuitcase)
        {
            InventoryMenu menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
            menu.RemoveItemIfExists("Quest_suitcaseVacation");
            
            Dictionary data = saveNode.InventoryData;
            
            
            if (data.Contains("weapon"))
            {
                suitcase.ChestHandler.AddNewItem(data["weapon"].ToString());
                suitcaseEmpty = false;
            }
            if (data.Contains("cloth") && data["cloth"].ToString() != "empty")
            {
                suitcase.ChestHandler.AddNewItem(data["cloth"].ToString());
                suitcaseEmpty = false;
            }
            if (data.Contains("artifact"))
            {
                suitcase.ChestHandler.AddNewItem(data["artifact"].ToString());
                suitcaseEmpty = false;
            }
            
            if (suitcaseEmpty)
            {
                Global.AddDeletedObject(suitcase.Name);
                suitcase.QueueFree();
                
                base._on_activate_trigger();
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

        base._on_activate_trigger();
    }
}
