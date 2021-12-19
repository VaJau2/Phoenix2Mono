using System;
using Godot;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

public class EnemiesManager : Node, ISavable
{
    [Export] private int MIN_ENEMIES_COUNT_TO_SPAWN = 14;
    [Export] private float SPAWN_COOLDOWN = 5f;
    private const int MIN_HEALTH_TO_SPAWN = 20;
    private const float SPAWN_MIN_DIST = 15f;
    private const float SPAWN_MAX_DIST = 60f;

    [Export] public bool hasAlarm;
    [Export] public bool isAudi3D;
    [Export] public List<NodePath> alarmSoundsPath;
    [Export] private List<PackedScene> npcPrefabs;

    public List<NPC> enemies = new List<NPC>();
    public bool maySpawn = true;
    public bool isAlarming { get; private set; }
    Player player => Global.Get().player;
    private List<AudioPlayerCommon> audi;
    private List<VisibilityNotifier> spawnPoints;
    private float spawnCooldown;
    
    [Signal] public delegate void AlarmStarted();
    [Signal] public delegate void AlarmEnded();

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

    private VisibilityNotifier GetRandomSpawnPoint()
    {
        return (from tempSpawner in spawnPoints
            let tempDistance = player.GlobalTransform.origin.DistanceTo(tempSpawner.GlobalTransform.origin)
            where tempDistance > SPAWN_MIN_DIST && tempDistance < SPAWN_MAX_DIST && !tempSpawner.IsOnScreen()
            select tempSpawner).FirstOrDefault();
    }

    private void SpawnEnemy(VisibilityNotifier point)
    {
        if (point == null) return;
        if (point.IsOnScreen()) return;
        if (npcPrefabs.Count == 0) return;
        if (player.Health < MIN_HEALTH_TO_SPAWN) return;

        Random rand = new Random();
        int randNum = rand.Next(npcPrefabs.Count);
        PackedScene npcPrefab = npcPrefabs[randNum];

        if (!(npcPrefab.Instance() is NpcWithWeapons npcInstance)) return;
        npcInstance.Name = "Created_" + npcInstance.Name;
        GetNode<Node>("/root/Main/Scene/npc").AddChild(npcInstance);
        npcInstance.GlobalTransform =
            Global.setNewOrigin(npcInstance.GlobalTransform, point.GlobalTransform.origin);
        npcInstance.Rotation = new Vector3(0, point.Rotation.y, 0);
        npcInstance.SetNewStartPos(point.GlobalTransform.origin);
        npcInstance.myStartRot = new Vector3(0, point.Rotation.y, 0);
        npcInstance.tempVictim = player;
        npcInstance.SetState(NPCState.Attack);

        enemies.Add(npcInstance);
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

        //спавнеры врагов
        spawnPoints = new List<VisibilityNotifier>();
        foreach (Node temp in GetNode<Node>("/root/Main/Scene/spawnPoints").GetChildren())
        {
            if (!(temp is VisibilityNotifier notifier)) continue;
            spawnPoints.Add(notifier);
        }
    }

    public override void _Process(float delta)
    {
        if (!isAlarming) return;
        if (!maySpawn) return;

        if (spawnCooldown > 0)
        {
            spawnCooldown -= delta;
            return;
        }

        if (enemies.Count >= MIN_ENEMIES_COUNT_TO_SPAWN) return;

        var spawnPoint = GetRandomSpawnPoint();
        SpawnEnemy(spawnPoint);
        spawnCooldown = SPAWN_COOLDOWN;
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"isAlarming", isAlarming},
            {"spawnCooldown", spawnCooldown},
            {"maySpawn", maySpawn}
        };
    }

    public void LoadData(Dictionary data)
    {
        maySpawn = Convert.ToBoolean(data["maySpawn"]);
        
        if (!hasAlarm) return;
        if (!data.Contains("isAlarming")) return;

        var alarming = Convert.ToBoolean(data["isAlarming"]);
        if (!alarming) return;

        spawnCooldown = Convert.ToSingle(data["spawnCooldown"]);
        StartAlarm();
    }
}