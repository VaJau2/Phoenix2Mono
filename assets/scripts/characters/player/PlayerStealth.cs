using Godot;
using System.Collections.Generic;

public class PlayerStealth : Node
{
    private Label StealthLabel;
    private string currentState = "safe";

    List<Character> seekEnemies = [];
    List<Character> attackEnemies = [];
    
    public override void _Ready()
    {
        StealthLabel = GetNode<Label>("/root/Main/Scene/canvas/stealthLabel");
    }

    public void SetLabelVisible(bool visible)
    {
        StealthLabel.Visible = visible;
        
        if (visible)
        {
            ChangeLabelState(currentState);
        }
    }

    private void checkEmpty()
    {
        for (int i = 0; i < seekEnemies.Count; i++)
        {
            if (seekEnemies.Count > i)
            {
                if (seekEnemies[i] == null || !IsInstanceValid(seekEnemies[i]))
                {
                    seekEnemies.RemoveAt(i);
                    continue;
                }
            }
        }

        for (int i = 0; i < attackEnemies.Count; i++)
        {
            if (attackEnemies.Count > i)
            {
                if (attackEnemies[i] == null || !IsInstanceValid(attackEnemies[i]))
                {
                    attackEnemies.RemoveAt(i);
                }
            }
        }

        if (attackEnemies.Count == 0)
        {
            if (seekEnemies.Count == 0)
            {
                ChangeLabelState("safe");
                StealthLabel.Modulate = Colors.White;
            }
            else
            {
                ChangeLabelState("caution");
                StealthLabel.Modulate = Colors.Orange;
            }
        }
    }

    public void AddAttackEnemy(Character enemy)
    {
        checkEmpty();
        if (!attackEnemies.Contains(enemy))
        {
            attackEnemies.Add(enemy);
            ChangeLabelState("danger");
            StealthLabel.Modulate = Colors.Red;
        }
    }

    public void RemoveAttackEnemy(Character enemy)
    {
        checkEmpty();
        if (attackEnemies.Contains(enemy))
        {
            attackEnemies.Remove(enemy);
            if (seekEnemies.Count == 0 && attackEnemies.Count == 0)
            {
                ChangeLabelState("safe");
                StealthLabel.Modulate = Colors.White;
            }
        }
    }

    public void AddSeekEnemy(Character enemy)
    {
        RemoveAttackEnemy(enemy);
        if (!seekEnemies.Contains(enemy))
        {
            seekEnemies.Add(enemy);
            if (attackEnemies.Count == 0)
            {
                ChangeLabelState("caution");
                StealthLabel.Modulate = Colors.Orange;
            }
        }
    }

    public void RemoveSeekEnemy(Character enemy)
    {
        RemoveAttackEnemy(enemy);
        if (seekEnemies.Contains(enemy))
        {
            seekEnemies.Remove(enemy);
            if (seekEnemies.Count == 0 && attackEnemies.Count == 0)
            {
                ChangeLabelState("safe");
                StealthLabel.Modulate = Colors.White;
            }
        }
    }

    private void ChangeLabelState(string state)
    {
        currentState = state;
        StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", state);
    }
}
