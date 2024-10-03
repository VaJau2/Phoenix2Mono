using Godot;
using Godot.Collections;

public class RoomManager : Node, ISavable
{
    public Room CurrentRoom { set; get; }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary()
        {
            {"current_room", CurrentRoom.Name}
        };

        for (int i = 0; i < CurrentRoom.activateTriggers.Count - 1; i++)
        {
            var trigger = CurrentRoom.activateTriggers[i];
            if (trigger == null) continue;

            saveData.Add($"{CurrentRoom.Name}{i}", trigger.IsActive);
        }

        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        if (!data.Contains("current_room")) return;
        var roomName = (string)data["current_room"];

        if (!string.IsNullOrEmpty(roomName))
        {
            foreach (var child in GetChildren())
            {
                if (child is not Room room) continue;
                room.Visible = room.Name == roomName;
            }
        }
        
        for (int i = 1; i < data.Count - 1; i++)
        {
            if (!data.Contains($"{CurrentRoom.Name}{i}"))
            {
                CurrentRoom.activateTriggers[i]._on_activate_trigger();
                continue;
            }
            
            var isActive = (bool)data[$"{CurrentRoom.Name}{i}"];
            CurrentRoom.activateTriggers[i].SetActive(isActive);
        }
    }
}
