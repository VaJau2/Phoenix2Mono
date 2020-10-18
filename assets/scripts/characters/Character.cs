using Godot;
using Godot.Collections;
using System;

public abstract class Character : KinematicBody
{
    [Export]
    public int Health;
    public int HealthMax {get; private set;}

    public Character() {
        HealthMax = Health;
    }

    protected void decreaseHealth(int decrease) {
        Health -= decrease;
        Health = Mathf.Clamp(Health, 0, HealthMax);
    }

    /// <summary>
    /// Метод должен будет использоваться во время сохранения, когда игра проходит по всем Character
    /// код загрузки лежит в global.cs
    /// </summary>
    public Dictionary GetSaveData() {
        Dictionary savingData = new Dictionary
        {
            {"pos_x", GlobalTransform.origin.x.ToString()},
            {"pos_y", GlobalTransform.origin.y.ToString()},
            {"pos_z", GlobalTransform.origin.z.ToString()},
            {"rot_x", GlobalTransform.basis.GetEuler().x.ToString()},
            {"rot_y", GlobalTransform.basis.GetEuler().y.ToString()},
            {"rot_z", GlobalTransform.basis.GetEuler().z.ToString()},
            {"health", Health.ToString()},
        };

        return savingData;
    }

    /// <summary>
    /// Метод должен будет использоваться во время загрузки, когда игра проходит по всем Character
    /// </summary>
    public void LoadData(Dictionary data) {
        Vector3 newPos = new Vector3((float)data["pos_x"], (float)data["pos_y"], (float)data["pos_z"]);
        Vector3 newRot = new Vector3((float)data["rot_x"], (float)data["rot_y"], (float)data["rot_z"]);

        Basis newBasis = new Basis(newRot);
        Transform newTransform = new Transform(newBasis, newPos);
        GlobalTransform = newTransform;

        Health = (int)data["health"];
    }
}
