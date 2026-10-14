using Godot;
using Godot.Collections;

public partial class SavePosition : Node3D, ISavable
{
    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "pos", GlobalTranslation },
            { "rot", GlobalRotation },
            { "scale", Scale }
        };
    }

    public void LoadData(Dictionary data)
    {
        GlobalTranslation = data["pos"].ToString().ParseToVector3();
        GlobalRotation = data["rot"].ToString().ParseToVector3();
        Scale = data["scale"].ToString().ParseToVector3();
    }
}
