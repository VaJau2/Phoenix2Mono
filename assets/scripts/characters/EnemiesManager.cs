using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public class EnemiesManager : Node, ISavable
{
    [Export] private float TRIGGER_COOLDOWN = 5f;
    private const float PLAYER_MIN_DIST = 15f;
    private const float PLAYER_MAX_DIST = 60f;
    [Export] public bool hasAlarm;
    [Export] public bool isAudi3D;
    [Export] public List<NodePath> alarmSoundsPath;

    public List<NPC> enemies = new List<NPC>();
    
    public bool isAlarming { get; private set; }
    Player player => Global.Get().player;
    private List<AudioPlayerCommon> audi;
    
    private float triggerCooldown;

    [Signal] public delegate void AlarmStarted();
    [Signal] public delegate void AlarmEnded();

    [Signal] public delegate void PlayerStealthSafe();
    [Signal] public delegate void PlayerStealthCaution();
    [Signal] public delegate void PlayerStealthDanger();

    private void StartAlarm()
    {
        if (!hasAlarm) return;
        if (isAlarming) return;
        foreach (var tempAudi in audi)
        {
            tempAudi.Play();
        }

        isAlarming = true;
        EmitSignal(nameof(AlarmStarted));
        SetProcess(true);
    }

    public void StopAlarm()
    {
        foreach (var tempAudi in audi)
        {
            tempAudi.Stop();
        }

        isAlarming = false;
        EmitSignal(nameof(AlarmEnded));
        SetProcess(false);
    }

    private bool PositionIsCloseToPlayer(Vector3 position)
    {
        var tempDistance = player.GlobalTransform.origin.DistanceTo(position);
        return tempDistance > PLAYER_MIN_DIST && tempDistance < PLAYER_MAX_DIST;
    }


    private void MakeCloseEnemyAttack()
    {
        foreach (var enemy in enemies.Where(enemy => 
            enemy.state == NPCState.Idle 
            && PositionIsCloseToPlayer(enemy.GlobalTransform.origin))
        )
        {
            enemy.tempVictim = player;
            enemy.SetState(NPCState.Attack);
            break;
        }
    }

    public void LoudShoot(float distance, Vector3 shootPos)
    {
        foreach (var temp in from temp in enemies
            where IsInstanceValid(temp) && temp.Health > 0 && temp.state != NPCState.Attack
            let enemyPos = temp.GlobalTransform.origin
            where shootPos.DistanceTo(enemyPos) <= distance
            select temp)
        {
            temp.SetLastSeePos(shootPos);
            temp.SetState(NPCState.Search);
        }

        if (!hasAlarm) return;
        StartAlarm();
    }

    public override void _Ready()
    {
        SetProcess(false);

        //враги с уровня
        foreach (Node temp in GetChildren())
        {
            if (!(temp is NPC npc) || temp.IsQueuedForDeletion()) continue;
            if (npc.relation == Relation.Enemy)
            {
                enemies.Add(npc);
            }
        }

        if (!hasAlarm) return;

        //сирены на уровне
        audi = new List<AudioPlayerCommon>();
        foreach (var tempAlarmPath in alarmSoundsPath)
        {
            audi.Add(new AudioPlayerCommon(isAudi3D, tempAlarmPath, this));
        }
    }

    public override void _Process(float delta)
    {
        if (!isAlarming) return;

        if (triggerCooldown > 0)
        {
            triggerCooldown -= delta;
            return;
        }
        
        //уже наспавненные враги тоже будут атаковать
        MakeCloseEnemyAttack();
        triggerCooldown = TRIGGER_COOLDOWN;
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"isAlarming", isAlarming},
        };
    }

    public void LoadData(Dictionary data)
    {
        if (!hasAlarm) return;
        if (!data.Contains("isAlarming")) return;

        var alarming = Convert.ToBoolean(data["isAlarming"]);
        if (!alarming) return;
        
        StartAlarm();
    }
}