using System;
using Godot;
using Godot.Collections;

namespace FurnStairs;

public class FurnStairsSpawner
{
    private Dictionary<string, string> prefabsByKeys = new()
    {
        { "tableWood", "res://objects/props/furniture/table.tscn" },
        { "tableMetal", "res://objects/props/furniture/stable-table-2.tscn" },
        { "sofa", "res://objects/props/furniture/sofa.tscn" },
        { "sofaBlack", "res://objects/props/furniture/lab-sofa.tscn" },
        { "chair", "res://objects/props/furniture/chair.tscn" },
    };

    public Spatial SpawnFurnByItemCode(string furnItemCode)
    {
        if (!prefabsByKeys.TryGetValue(furnItemCode, out var prefabPath))
        {
            throw new Exception("there is no furn prefab by key " + furnItemCode);
        }
        
        var prefab = GD.Load<PackedScene>(prefabPath);
        if (prefab.Instance() is not Spatial furn) return null;
        
        furn.AddToGroup("savable");
        furn.Name = "Created_" + furn.Name;
        return furn;
    }
}
