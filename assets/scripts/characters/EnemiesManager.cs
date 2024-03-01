using System;
using System.Collections.Generic;
using Godot;
using System.Linq;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class EnemiesManager : Node, ISavable
{
    [Export] private float TRIGGER_COOLDOWN = 5f;
    private const float PLAYER_MIN_DIST = 15f;
    private const float PLAYER_MAX_DIST = 60f;
    [Export] public bool hasAlarm;
    [Export] public bool isAudi3D;
    [Export] public Array alarmSoundsPath;

    public Array<NPC> enemies = [];
    
    public bool isAlarming { get; private set; }
    Player player => Global.Get().player;
    private List<AudioPlayerCommon> audi;
    
    private float triggerCooldown;

    [Signal] public delegate void AlarmStartedEventHandler();
    [Signal] public delegate void AlarmEndedEventHandler();

    [Signal] public delegate void PlayerStealthSafeEventHandler();
    [Signal] public delegate void PlayerStealthCautionEventHandler();
    [Signal] public delegate void PlayerStealthDangerEventHandler();

    private void StartAlarm()
    {
        if (!hasAlarm) return;
        if (isAlarming) return;
        foreach (var tempAudi in audi)
        {
            tempAudi.Play();
        }

        isAlarming = true;
        EmitSignal(SignalName.AlarmStarted);
        SetProcess(true);
    }

    public void StopAlarm()
    {
        foreach (var tempAudi in audi)
        {
            tempAudi.Stop();
        }

        isAlarming = false;
        EmitSignal(SignalName.AlarmEnded);
        SetProcess(false);
    }

    private bool PositionIsCloseToPlayer(Vector3 position)
    {
        var tempDistance = player.GlobalTransform.Origin.DistanceTo(position);
        return tempDistance > PLAYER_MIN_DIST && tempDistance < PLAYER_MAX_DIST;
    }


    private void MakeCloseEnemyAttack()
    {
        foreach (var enemy in enemies.Where(enemy => 
            enemy.state == NPCState.Idle 
            && PositionIsCloseToPlayer(enemy.GlobalTransform.Origin))
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
            let enemyPos = temp.GlobalTransform.Origin
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
            audi.Add(new AudioPlayerCommon(isAudi3D, tempAlarmPath.ToString(), this));
        }
    }

    public override void _Process(double delta)
    {
        if (!isAlarming) return;

        if (triggerCooldown > 0)
        {
            triggerCooldown -= (float)delta;
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
        if (!data.ContainsKey("isAlarming")) return;

        var alarming = Convert.ToBoolean(data["isAlarming"]);
        if (!alarming) return;
        
        StartAlarm();
    }
}