using Godot;
using Godot.Collections;

//нод, сохраняемый во время перехода между уровнями
//хранит в себе данные инвентаря
public class SaveNode: Node, ISavable
{
    public Dictionary InventoryData = new Dictionary();
    public Dictionary SavedVariables = new Dictionary();
    
    //иногда игрок может лишиться инвентаря и вернуть его на следующем уровне
    //для сохранения старого и нового инвентаря сохраненный раннее инвентарь клонируется в новый объект
    private const string CLONE_NAME = "SaveNode2";
    
    public void CloneSaveData(Node parent)
    {
        var newSaveNode = new SaveNode();
        parent.AddChild(newSaveNode);
        newSaveNode.Name = CLONE_NAME;
        newSaveNode.InventoryData = InventoryData.Duplicate();
        newSaveNode.SavedVariables = SavedVariables.Duplicate();
    }

    public void CheckClonedSaveData()
    {
        var oldSaveNode = GetNodeOrNull<SaveNode>("/root/Main/" + CLONE_NAME);
        if (oldSaveNode == null) return;

        InventoryData = oldSaveNode.InventoryData;
        SavedVariables = oldSaveNode.SavedVariables;
        oldSaveNode.QueueFree();
    }
    
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
