using Godot;
using Godot.Collections;

public static class ItemsList {
    public static Array<PackedScene> Items = new Array<PackedScene>() 
    {
        GD.Load<PackedScene>("res://objects/items/HealPotion.tscn"),
        GD.Load<PackedScene>("res://objects/items/10mmAmmo.tscn"),
        GD.Load<PackedScene>("res://objects/items/20mmAmmo.tscn"),
        GD.Load<PackedScene>("res://objects/items/357mmAmmo.tscn"),
        GD.Load<PackedScene>("res://objects/items/307mmAmmo.tscn"),
    };
}