using Godot;
using Godot.Collections;

public class DragonSpawnTrigger : TriggerBase
{
    [Export] private PackedScene dragonPrefab;
    [Export] private NodePath spawnPointPath;
    [Export] public Array<NodePath> patrolArray;
    private Spatial spawnPoint;
    
    //0 - путь до триггера, строка
    //1 - сигнал нпц, строка
    //2 - метод триггера, строка
    //3 - binds, массив
    [Export] private Array triggerConnections;
    
    [Signal]
    public delegate void Spawned(NPC dragon);

    public override void _Ready()
    {
        if (spawnPointPath != null)
        {
            spawnPoint = GetNode<Spatial>(spawnPointPath);
        }
        base._Ready();
    }

    public override void SetActive(bool newActive)
    {
        _on_activate_trigger();
        base.SetActive(newActive);
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        var dragon = SpawnNpc();
        EmitSignal(nameof(Spawned), dragon);

        base._on_activate_trigger();
    }
    
    private NPC SpawnNpc()
    {
        if (dragonPrefab.Instance() is not NPC npcInstance)
        {
            return null;
        }
        
        npcInstance.Name = "Created_" + npcInstance.Name;
        npcInstance.patrolArray = patrolArray;
        GetNode<Node>("/root/Main/Scene/npc").AddChild(npcInstance);
        npcInstance.GlobalTransform = Global.SetNewOrigin(npcInstance.GlobalTransform, spawnPoint.GlobalTransform.origin);
        npcInstance.Rotation = new Vector3(0, spawnPoint.Rotation.y,0);

        if (triggerConnections == null)
        {
            return npcInstance;
        }
        
        foreach (var triggerDataPrimary in triggerConnections)
        {
            if (triggerDataPrimary is not Array { Count: 4 } triggerData) continue;

            var trigger = GetNode(triggerData[0].ToString());
            var signal = triggerData[1].ToString();
            var method = triggerData[2].ToString();
            var binds = triggerData[3] as Array;
            npcInstance.Connect(signal, trigger, method, binds);
        }

        return npcInstance;
    }
    
    public void _on_body_entered(Node body)
    {
        if (body is Player)
        {
            _on_activate_trigger();
        }
    }
}
