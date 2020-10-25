using Godot;
using Godot.Collections;
using System;

public class FurnChest: FurnBase {

    const float ITEM_DROP_CHANCE = 0.65f;

    [Export]
    public Vector3 dropSide;
    [Export]
    public float itemHeight = 2f;
    [Export]
    public PackedScene itemPrefab;
    [Export]
    public string keyName = "";
    [Export]
    public string pickKeyTextLink = "";

    bool itemDropped = false;

    Random rand = new Random();

    private async void dropItem() {
        Spatial item;
        if (itemPrefab == null) {
            Array<PackedScene> items = ItemsList.Items;
            var randI = rand.Next(0, items.Count);
            item = (Spatial)items[randI].Instance();
        } else {
            item = (Spatial)itemPrefab.Instance();
        }

        if (item != null) {
            if (item is ItemKey) {
                var key = item as ItemKey;
                key.KeyName = keyName;
                key.KeyPickTextLink = pickKeyTextLink;
            }

            GetNode("/root/Main/Scene/items").AddChild(item);
            Vector3 oldPos = GlobalTransform.origin;
            item.GlobalTransform = Global.setNewOrigin(item.GlobalTransform, oldPos);
            
            var wr = WeakRef(item);
            while(wr.GetRef() != null && itemHeight > 0) {
                Vector3 newPos = item.GlobalTransform.origin;
                newPos.x += dropSide.x * 0.1f;
                newPos.y -= 0.1f;
                newPos.z += dropSide.z * 0.1f;
                item.GlobalTransform = Global.setNewOrigin(item.GlobalTransform, newPos);
                itemHeight -= 0.1f;
                await ToSignal(GetTree(), "idle_frame");
            }
        }
    }


    public override void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null)
    {
        base.ClickFurn();
        if (IsOpen) {
            if (!itemDropped) {
                if (GD.Randf() < ITEM_DROP_CHANCE || itemPrefab != null) {
                    dropItem();
                }
                itemDropped = true;
            }
        }
    }
}