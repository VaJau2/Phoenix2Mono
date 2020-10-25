using Godot;
using System.Collections.Generic;

public static class MatNames {
    private static Dictionary<float, string> matNames = new Dictionary<float, string>() 
    {
        {0.1f, "stairs"},
        {0.2f, "blood"},
        {0.3f, "glass"},
        {0.4f, "grass"},
        {0.5f, "dirt"},
        {0.6f, "wood"},
        {0.8f, "fence"},
        {1, "stone"} //эта херня каким-то образом игнорит 0.7 и 0.9 \_(._.)_/
    };

    public static string GetMatName(float friction) {
        if (!matNames.ContainsKey(friction)) {
            GD.PrintErr(friction.ToString() + " is not in materials array :/");
        }
        return matNames[friction];
    }
}