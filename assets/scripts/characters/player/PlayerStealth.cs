using Godot;
using System.Collections.Generic;

public class PlayerStealth: Node {
    public StealthStage Stage;
    private Label StealthLabel;

    public override void _Ready()
    {
        StealthLabel = GetNode<Label>("/root/Main/Scene/canvas/stealthLabel");
        StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "safe");
    }

    public void SetLabelVisible(bool visible) 
    {
        StealthLabel.Visible = visible;
    }

    List<Character> seekEnemies = new List<Character>();
    List<Character> attackEnemies = new List<Character>();

    private void checkEmpty() 
    {
        foreach(Character enemy in seekEnemies) {
            if (enemy == null) {
                seekEnemies.Remove(enemy);
                continue;
            }

            if (!IsInstanceValid(enemy)) {
                seekEnemies.Remove(enemy);
            }
        }

        foreach(Character enemy in attackEnemies) {
            if (enemy == null) {
                attackEnemies.Remove(enemy);
                continue;
            }

            if (!IsInstanceValid(enemy)) {
                attackEnemies.Remove(enemy);
            }
        }

        if (attackEnemies.Count == 0) {
            if (seekEnemies.Count == 0) {
                StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "safe");
                StealthLabel.Modulate = Colors.White;
                Stage = StealthStage.Safe;
            } else {
                StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "caution");
                StealthLabel.Modulate = Colors.Orange;
                Stage = StealthStage.Caution;
            }
        }
    }

    public void AddAttackEnemy(Character enemy)
    {
        checkEmpty();
        if (!attackEnemies.Contains(enemy)) 
        {
            attackEnemies.Add(enemy);
            StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "danger");
            StealthLabel.Modulate = Colors.Red;
            Stage = StealthStage.Danger;
        }
    }

    public void RemoveAttackEnemy(Character enemy) 
    {
        checkEmpty();
        if (attackEnemies.Contains(enemy)) {
            attackEnemies.Remove(enemy);
            if (seekEnemies.Count == 0 && attackEnemies.Count == 0) {
                StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "safe");
                StealthLabel.Modulate = Colors.White;
                Stage = StealthStage.Safe;
            }
        }
    }

    public void AddSeekEnemy(Character enemy) 
    {
        RemoveAttackEnemy(enemy);
        if (!seekEnemies.Contains(enemy)) {
            seekEnemies.Add(enemy);
            if (attackEnemies.Count == 0) {
                StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "caution");
                StealthLabel.Modulate = Colors.Orange;
                Stage = StealthStage.Caution;
            }
        }
    }

    public void RemoveSeekEnemy(Character enemy)
    {
        checkEmpty();
        if (seekEnemies.Contains(enemy)) {
            seekEnemies.Remove(enemy);
            if (seekEnemies.Count == 0 && attackEnemies.Count == 0) {
                StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "safe");
                StealthLabel.Modulate = Colors.White;
                Stage = StealthStage.Safe;
            }
        }
    }
}



public enum StealthStage 
{
    Safe,
    Caution,
    Danger
}