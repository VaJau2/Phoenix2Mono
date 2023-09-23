using Godot;
using Godot.Collections;
using System;

//управляет статичными мешами гильз
//ограничивает их общее количество
//сохраняет и загружает сохраненные меши гильз
public class ShellsManager : Spatial, ISavable
{
    [Export]
    private int shellsLimit = 20;

    public void AddShell(MeshInstance shell)
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
        foreach(MeshInstance shell in GetChildren())
        {
            savingData.Add(shell.Name, new Dictionary
            {
                {"meshPath", shell.Mesh.ResourcePath },
                {"pos_x", shell.GlobalTransform.origin.x},
                {"pos_y", shell.GlobalTransform.origin.y},
                {"pos_z", shell.GlobalTransform.origin.z},
                {"rot_x", shell.GlobalTransform.basis.GetEuler().x},
                {"rot_y", shell.GlobalTransform.basis.GetEuler().y},
                {"rot_z", shell.GlobalTransform.basis.GetEuler().z},
                {"scale", shell.Scale.x }
            });
        }
        return savingData;
    }

    public void LoadData(Dictionary data)
    {
        foreach(string shellName in data.Keys)
        {
            if (data[shellName] is Dictionary shellData)
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

        Basis newBasis = new Basis(newRot);
        Transform newTransform = new Transform(newBasis, newPos);

        MeshInstance shell = new MeshInstance();
        Mesh newMesh = GD.Load<Mesh>(shellData["meshPath"].ToString());
        shell.Mesh = newMesh;
        AddChild(shell);
        shell.GlobalTransform = newTransform;
        shell.Scale = newScale;
    }
}
