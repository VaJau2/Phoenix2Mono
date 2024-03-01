using Godot;
using Godot.Collections;
using System;

//управляет статичными мешами гильз
//ограничивает их общее количество
//сохраняет и загружает сохраненные меши гильз
public partial class ShellsManager : Node3D, ISavable
{
    [Export]
    private int shellsLimit = 20;

    public void AddShell(MeshInstance3D shell)
    {
        AddChild(shell);
        if (GetChildCount() >= shellsLimit)
        {
            GetChild(0).QueueFree();
        }
    }

    public Dictionary GetSaveData()
    {
        Dictionary savingData = new Dictionary();
        foreach(MeshInstance3D shell in GetChildren())
        {
            savingData.Add(shell.Name, new Dictionary
            {
                {"meshPath", shell.Mesh.ResourcePath },
                {"pos_x", shell.GlobalTransform.Origin.X},
                {"pos_y", shell.GlobalTransform.Origin.Y},
                {"pos_z", shell.GlobalTransform.Origin.Z},
                {"rot_x", shell.GlobalTransform.Basis.GetEuler().X},
                {"rot_y", shell.GlobalTransform.Basis.GetEuler().Y},
                {"rot_z", shell.GlobalTransform.Basis.GetEuler().Z},
                {"scale", shell.Scale.X }
            });
        }
        return savingData;
    }

    public void LoadData(Dictionary data)
    {
        foreach(string shellName in data.Keys)
        {
            if (data[shellName].AsGodotDictionary() is { } shellData)
            {
                CallDeferred(nameof(LoadShell), shellData);
            }
        }
    }

    private void LoadShell(Dictionary shellData)
    {
        Vector3 newPos = new Vector3(Convert.ToSingle(shellData["pos_x"]), Convert.ToSingle(shellData["pos_y"]), Convert.ToSingle(shellData["pos_z"]));
        Vector3 newRot = new Vector3(Convert.ToSingle(shellData["rot_x"]), Convert.ToSingle(shellData["rot_y"]), Convert.ToSingle(shellData["rot_z"]));
        float scaleValue = Convert.ToSingle(shellData["scale"]);
        Vector3 newScale = new Vector3(scaleValue, scaleValue, scaleValue);

        Basis newBasis = Basis.FromEuler(newRot);
        Transform3D newTransform = new Transform3D(newBasis, newPos);

        MeshInstance3D shell = new MeshInstance3D();
        Mesh newMesh = GD.Load<Mesh>(shellData["meshPath"].ToString());
        shell.Mesh = newMesh;
        AddChild(shell);
        shell.GlobalTransform = newTransform;
        shell.Scale = newScale;
    }
}
