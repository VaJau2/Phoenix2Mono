using Godot;
using Godot.Collections;

//нод, сохраняемый во время перехода между уровнями
//хранит в себе данные инвентаря
public class SaveNode: Node, ISavable
{
    public Dictionary InventoryData = new Dictionary();
    public Dictionary SavedVariables = new Dictionary();
    
    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"inventory", InventoryData},
            {"savedVariables", SavedVariables},
        };
    }

    public void LoadData(Dictionary data)
    {
        if (data["inventory"] is Dictionary inventory)
        {
            InventoryData = inventory;
        }

        if (data["savedVariables"] is Dictionary variables)
        {
            SavedVariables = variables;
        }
    }
}
