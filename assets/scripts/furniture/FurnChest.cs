using Godot;
using Godot.Collections;
using System;

public class FurnChest: FurnBase {

    InventoryMenu menu;
    Random rand = new Random();

    public override void _Ready()
    {
        base._Ready();
        menu = GetNode<InventoryMenu>("/root/Main/Scene/canvas/inventory");
    }


    public override void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null)
    {
        base.ClickFurn();
        if (!menu.isOpen) {
            menu.OpenMenu(InventoryMode.Chest);
            menu.Connect("MenuIsClosed", this, nameof(CloseFurn));
        }
    }

    public void CloseFurn()
    {
        base.ClickFurn();
        menu.Disconnect("MenuIsClosed", this, nameof(CloseFurn));
    }
}