using Godot;
using System.Collections.Generic;

public class Room : SaveActive
{
    [Export] float depth = 100 / 1.5f;

    RadioController radioController;
    [Export] List<NodePath> radioPaths;
    List<RadioBase> radioList = new List<RadioBase>();

    public override void _Ready()
    {
        if (radioPaths.Count > 0)
        {
            foreach (NodePath radioPath in radioPaths)
            {
                RadioBase radio = GetNode<RadioBase>(radioPath);
                radio.depthOfRoom = depth;
                radio.inRoom = true;
                radioList.Add(radio);
            }

            radioController = GetNode<RadioController>("/root/Main/Scene/RadioController");
        }
    }

    public void Enter()
    {
        if (radioController != null) radioController.EnterToRoom(radioList);
        radioController.currentRoom = GetPath();
    }

    public void Exit()
    {
        if (radioController != null) radioController.ExitFromRoom(radioList);
        radioController.currentRoom = null;
    }
}
