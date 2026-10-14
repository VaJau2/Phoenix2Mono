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
                { "meshPath", shell.Mesh.ResourcePath },
                { "pos", shell.GlobalTransform.Origin },
                { "rot", shell.GlobalTransform.Basis.GetEuler() },
                { "scale", shell.Scale.X }
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
        Transform3D newTransform = new Transform3D(newBasis, newPos);

        MeshInstance3D shell = new MeshInstance3D();
        Mesh newMesh = GD.Load<Mesh>(GetMeshPath(shellData["meshPath"].ToString()));
        shell.Mesh = newMesh;
        AddChild(shell);
        shell.GlobalTransform = newTransform;
        shell.Scale = newScale;
    }

    //в старых сохранениях меши гильз лежат в бинарных .mesh,
    //которые были переведены в .tres при подготовке к Godot 4
    private static string GetMeshPath(string savedPath)
    {
        if (ResourceLoader.Exists(savedPath)) return savedPath;
        return savedPath.BaseName() + ".tres";
    }
}
