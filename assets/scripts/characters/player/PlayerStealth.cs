using System;
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

    private void SetNewStage(StealthStage newStage)
    {
        Stage = newStage;
        var enemiesManager = GetNodeOrNull<EnemiesManager>("/root/Main/Scene/npc");
        if (!IsInstanceValid(enemiesManager)) return;
        
        switch (newStage)
        {
            case StealthStage.Caution:
                enemiesManager.EmitSignal(nameof(EnemiesManager.PlayerStealthCaution));
                break;
            case StealthStage.Danger:
                enemiesManager.EmitSignal(nameof(EnemiesManager.PlayerStealthDanger));
                break;
            case StealthStage.Safe:
                enemiesManager.EmitSignal(nameof(EnemiesManager.PlayerStealthSafe));
                break;
        }
    }

    private void checkEmpty() 
    {
        for(int i = 0; i < seekEnemies.Count; i++) {
            if (seekEnemies.Count > i) {
                if (seekEnemies[i] == null || !IsInstanceValid(seekEnemies[i])) {
                    seekEnemies.RemoveAt(i);
                    continue;
                }
            }
        }

        for(int i = 0; i < attackEnemies.Count; i++) {
            if (attackEnemies.Count > i) {
                if (attackEnemies[i] == null || !IsInstanceValid(attackEnemies[i])) {
                    attackEnemies.RemoveAt(i);
                    continue;
                }
            }
        }

        if (attackEnemies.Count == 0) {
            if (seekEnemies.Count == 0) {
                StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "safe");
                StealthLabel.Modulate = Colors.White;
                SetNewStage(StealthStage.Safe);
            } else {
                StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "caution");
                StealthLabel.Modulate = Colors.Orange;
                SetNewStage(StealthStage.Caution);
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
            SetNewStage(StealthStage.Danger);
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
                SetNewStage(StealthStage.Safe);
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
                SetNewStage(StealthStage.Caution);
            }
        }
    }

    public void RemoveSeekEnemy(Character enemy)
    {
        RemoveAttackEnemy(enemy);
        if (seekEnemies.Contains(enemy)) {
            seekEnemies.Remove(enemy);
            if (seekEnemies.Count == 0 && attackEnemies.Count == 0) {
                StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", "safe");
                StealthLabel.Modulate = Colors.White;
                SetNewStage(StealthStage.Safe);
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