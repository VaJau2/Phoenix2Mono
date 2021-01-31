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

    private bool mayOpen => (IsOpen == menu.isOpen);

    public override void ClickFurn(AudioStreamSample openSound = null, float timer = 0, string openAnim = null)
    {
        if (!mayOpen) return;

        if (IsOpen) { 
            //если мебель и меню открыты
            //меню закроется и закроет мебель
            menu.CloseMenu();
        } else {
            base.ClickFurn();
            menu.ChangeMode(NewInventoryMode.Chest);
            menu.OpenMenu();
            menu.Connect("MenuIsClosed", this, nameof(CloseFurn));
        }
    }

    public void CloseFurn()
    {
        base.ClickFurn();
        menu.Disconnect("MenuIsClosed", this, nameof(CloseFurn));
    }
}