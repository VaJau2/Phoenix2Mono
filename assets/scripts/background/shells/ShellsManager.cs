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
                { "meshPath", shell.Mesh.ResourcePath },
                { "pos", shell.GlobalTransform.origin },
                { "rot", shell.GlobalTransform.basis.GetEuler() },
                { "scale", shell.Scale.x }
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
        Vector3 newPos = shellData["pos"].ToString().ParseToVector3();
        Vector3 newRot = shellData["rot"].ToString().ParseToVector3();
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
