using System.Collections.Generic;
using System.Linq;
using Godot;

//Названия клавиш в Godot 4 частично изменились (Control -> Ctrl, BackSpace -> Backspace, PrintScreen -> Print),
//а иконки кнопок (icons/buttons/<название>.png) и сохранённые настройки управления используют названия из Godot 3
public static class KeyNames
{
    private static readonly Dictionary<string, string> godot4ToProject = new()
    {
        { "Ctrl", "Control" },
        { "Backspace", "BackSpace" },
        { "Print", "PrintScreen" },
    };

    public static string GetName(Key key)
    {
        var name = OS.GetKeycodeString(key);
        return godot4ToProject.TryGetValue(name, out var projectName) ? projectName : name;
    }

    public static Key FromName(string name)
    {
        var godot4Name = godot4ToProject.FirstOrDefault(pair => pair.Value == name).Key ?? name;
        return OS.FindKeycodeFromString(godot4Name);
    }
}
