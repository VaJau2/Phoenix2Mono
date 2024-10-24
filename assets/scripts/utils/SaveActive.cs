using Godot;
using Godot.Collections;

//нод, добавляющий в сохранение информацию о своей активности
//(помещение)
public class SaveActive : Spatial, ISavable
{
    public Dictionary GetSaveData()
    {
        return new Dictionary()
        {
            {"active", Visible}
        };
    }

    public void LoadData(Dictionary data)
    {
        Visible = System.Convert.ToBoolean(data["active"]);
    }
}
