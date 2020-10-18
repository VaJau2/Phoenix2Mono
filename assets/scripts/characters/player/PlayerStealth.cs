using Godot;
using System.Collections.Generic;

public class PlayerStealth {
    public StealthStage Stage;
    private Label StealthLabel;

    public PlayerStealth(Label stealthLabel) {
        StealthLabel = stealthLabel;
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
            var wr = WeakRef.WeakRef(enemy);
            if (wr.GetRef() == null) {
                seekEnemies.Remove(enemy);
            }
        }

        foreach(Character enemy in attackEnemies) {
            var wr = WeakRef.WeakRef(enemy);
            if (wr.GetRef() == null) {
                attackEnemies.Remove(enemy);
            }
        }

        if (attackEnemies.Count == 0) {
            if (seekEnemies.Count == 0) {
                StealthLabel.Text = InterfaceLang.GetLang("inGame", "stealth", "safe");
                StealthLabel.Modulate = Colors.White;
                Stage = StealthStage.Safe;
            } else {
                StealthLabel.Text = InterfaceLang.GetLang("inGame", "stealth", "caution");
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
            StealthLabel.Text = InterfaceLang.GetLang("inGame", "stealth", "danger");
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
                StealthLabel.Text = InterfaceLang.GetLang("inGame", "stealth", "safe");
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
                StealthLabel.Text = InterfaceLang.GetLang("inGame", "stealth", "caution");
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
                StealthLabel.Text = InterfaceLang.GetLang("inGame", "stealth", "safe");
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