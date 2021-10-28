using Godot;
using System.Collections.Generic;

public static class MatNames {
    private static Dictionary<int, string> matNames = new Dictionary<int, string>() 
    {
        {1, "glass"},
        {2, "blood"},
        {20, "fence"},
        {30, "dirt"},
        {35, "snow"},
        {40, "wood"},
        {50, "grass"},
        {55, "stairs"},
        {56, "stone"},
        {60, "grass_stairs"},
        {70, "water"},
    };

    public static string GetMatName(float friction) {
        if (!matNames.ContainsKey((int)friction)) {
            GD.PrintErr(friction.ToString() + " is not in materials array :/");
        }
        return matNames[(int)friction];
    }
}