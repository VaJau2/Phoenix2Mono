using Godot;
using Godot.Collections;

public partial class DragonSpawnTrigger : TriggerBase
{
    [Export] private PackedScene dragonPrefab;
    [Export] private NodePath spawnPointPath;
    [Export] public Array<NodePath> patrolArray;
    private Node3D spawnPoint;
    
    //0 - путь до триггера, строка
    //1 - сигнал нпц, строка
    //2 - метод триггера, строка
    //3 - binds, массив
    [Export] private Array triggerConnections;
    
    [Signal]
    public delegate void SpawnedEventHandler(NPC dragon);

    public override void _Ready()
    {
        if (spawnPointPath != null)
        {
            spawnPoint = GetNode<Node3D>(spawnPointPath);
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
        EmitSignal(SignalName.Spawned, dragon);

        base._on_activate_trigger();
    }
    
    private NPC SpawnNpc()
    {
        if (dragonPrefab.Instantiate() is not NPC npcInstance)
        {
            return null;
        }
        
        npcInstance.Name = "Created_" + npcInstance.Name;
        npcInstance.patrolArray = patrolArray;
        GetNode<Node>("/root/Main/Scene/npc").AddChild(npcInstance);
        npcInstance.GlobalTransform = Global.SetNewOrigin(npcInstance.GlobalTransform, spawnPoint.GlobalTransform.Origin);
        npcInstance.Rotation = new Vector3(0, spawnPoint.Rotation.Y,0);

        if (triggerConnections == null)
        {
            return npcInstance;
        }
        
        foreach (var triggerDataPrimary in triggerConnections)
        {
            if (triggerDataPrimary.AsArrayOrNull() is not Array { Count: 4 } triggerData) continue;

            var trigger = GetNode(triggerData[0].ToString());
            var signal = triggerData[1].ToString();
            var method = triggerData[2].ToString();
            var binds = triggerData[3].AsArrayOrNull();
            if (binds is { Count: > 0 })
            {
                BoundCall.Connect(npcInstance, signal, trigger, method, binds);
            }
            else
            {
                npcInstance.Connect(signal, new Callable(trigger, method));
            }
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
