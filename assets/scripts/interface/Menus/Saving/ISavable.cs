using Godot.Collections;

public interface ISavable
{
    Dictionary GetSaveData();
    void LoadData(Dictionary data);
}
