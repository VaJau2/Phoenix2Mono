using System;
using Godot;
using Godot.Collections;

//Пока игрок находится внутри области триггера
//Тревога на уровне не будет включаться
public class BlockAlarmAreaTrigger : TriggerBase
{
    [Export] private NodePath managerPath;
    private EnemiesManager enemiesManager;
    private bool changedAlarm;

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["changedAlarm"] = changedAlarm;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        changedAlarm = Convert.ToBoolean(data["changedAlarm"]);
    }

    public override void _Ready()
    {
        enemiesManager = GetNode<EnemiesManager>(managerPath);
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        if (IsActive)
        {
            if (!enemiesManager.hasAlarm || enemiesManager.isAlarming) return;
            
            enemiesManager.hasAlarm = false;
            changedAlarm = true;
        }
        else
        {
            if (!changedAlarm) return;
            
            enemiesManager.hasAlarm = true;
            changedAlarm = false;
        }
    }

    public void _on_body_entered(Node body)
    {
        if (!(body is Player)) return;
        SetActive(true);
    }
    
    public void _on_body_exited(Node body)
    {
        if (!(body is Player)) return;
        SetActive(false);
    }
}
