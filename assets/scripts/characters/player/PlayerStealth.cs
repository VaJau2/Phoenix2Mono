using System;
using Godot;
using System.Collections.Generic;

public partial class PlayerStealth : Node
{
    public StealthStage Stage;
    private Label StealthLabel;
    private string currentState = "safe";

    List<Character> seekEnemies = new List<Character>();
    List<Character> attackEnemies = new List<Character>();
    
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

    private void SetNewStage(StealthStage newStage)
    {
        Stage = newStage;
        var enemiesManager = GetNodeOrNull<EnemiesManager>("/root/Main/Scene/npc");
        if (!IsInstanceValid(enemiesManager)) return;

        switch (newStage)
        {
            case StealthStage.Caution:
                enemiesManager.EmitSignal(nameof(EnemiesManager.PlayerStealthCautionEventHandler));
                break;
            case StealthStage.Danger:
                enemiesManager.EmitSignal(nameof(EnemiesManager.PlayerStealthDangerEventHandler));
                break;
            case StealthStage.Safe:
                enemiesManager.EmitSignal(nameof(EnemiesManager.PlayerStealthSafeEventHandler));
                break;
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
                    continue;
                }
            }
        }

        if (attackEnemies.Count == 0)
        {
            if (seekEnemies.Count == 0)
            {
                ChangeLabelState("safe");
                StealthLabel.Modulate = Colors.White;
                SetNewStage(StealthStage.Safe);
            }
            else
            {
                ChangeLabelState("caution");
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
            ChangeLabelState("danger");
            StealthLabel.Modulate = Colors.Red;
            SetNewStage(StealthStage.Danger);
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
                SetNewStage(StealthStage.Safe);
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
                SetNewStage(StealthStage.Caution);
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
                SetNewStage(StealthStage.Safe);
            }
        }
    }

    private void ChangeLabelState(string state)
    {
        currentState = state;
        StealthLabel.Text = InterfaceLang.GetPhrase("inGame", "stealth", state);
    }
}


public enum StealthStage
{
    Safe,
    Caution,
    Danger
}