using Godot;
using System;

public class HealthLabel : Label
{
    Player player;

    public override void _Process(float delta)
    {
        if (player == null) player = Global.Get().player;
        else {
            Text = player.Health.ToString();
        }
    }
}
