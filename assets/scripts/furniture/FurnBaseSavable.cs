using System;
using Godot.Collections;

public partial class FurnBaseSavable : FurnBase, ISavable
{
    public Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"open", IsOpen},
        };
    }

    public void LoadData(Dictionary data)
    {
        bool open = Convert.ToBoolean(data["open"]);
        if (open)
        {
            LoadOpenTrue(false);
        }
    }
}
