using Godot;
using System.Collections.Generic;

public static class MatNames {
    private static Dictionary<int, string> matNames = new Dictionary<int, string>() 
    {
        {55, "stairs"},
        {2, "blood"},
        {1, "glass"},
        {50, "grass"},
        {30, "dirt"},
        {40, "wood"},
        {20, "fence"},
        {56, "stone"},
        {60, "grass_stairs"}
    };

    public static string GetMatName(float friction) {
        if (!matNames.ContainsKey((int)friction)) {
            GD.PrintErr(friction.ToString() + " is not in materials array :/");
        }
        return matNames[(int)friction];
    }
}