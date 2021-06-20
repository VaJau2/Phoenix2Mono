using Godot;
using System;

public class DemoEnd : Control
{
    public override void _Process(float delta)
    {
        if (Input.IsActionPressed("jump"))
        {
            GetNode<LevelsLoader>("/root/Main").LoadLevel(0);
        }
    }
}
