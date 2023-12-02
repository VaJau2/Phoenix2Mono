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

        SpawnNpc();

        base._on_activate_trigger();
    }
    
    private void SpawnNpc()
    {
        if (!(dragonPrefab.Instance() is NPC npcInstance)) return;
        npcInstance.Name = "Created_" + npcInstance.Name;
        npcInstance.patrolArray = patrolArray;
        GetNode<Node>("/root/Main/Scene/npc").AddChild(npcInstance);
        npcInstance.GlobalTransform = Global.SetNewOrigin(npcInstance.GlobalTransform, spawnPoint.GlobalTransform.origin);
        npcInstance.Rotation = new Vector3(0, spawnPoint.Rotation.y,0);

        if (triggerConnections == null) return;
        foreach (var triggerDataPrimary in triggerConnections)
        {
            if (!(triggerDataPrimary is Array triggerData) || triggerData.Count != 4) continue;

            var trigger = GetNode(triggerData[0].ToString());
            var signal = triggerData[1].ToString();
            var method = triggerData[2].ToString();
            var binds = triggerData[3] as Array;
            npcInstance.Connect(signal, trigger, method, binds);
        }
    }
    
    public void _on_body_entered(Node body)
    {
        if (body is Player)
        {
            _on_activate_trigger();
        }
    }
}
