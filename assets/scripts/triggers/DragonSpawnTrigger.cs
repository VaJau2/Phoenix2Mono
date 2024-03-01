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
        OnActivateTrigger();
        base.SetActive(newActive);
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;

        SpawnNpc();

        base.OnActivateTrigger();
    }
    
    private void SpawnNpc()
    {
        if (dragonPrefab.Instantiate<NPC>() is not { } npcInstance) return;
        npcInstance.Name = "Created_" + npcInstance.Name;
        npcInstance.patrolArray = patrolArray;
        GetNode<Node>("/root/Main/Scene/npc").AddChild(npcInstance);
        npcInstance.GlobalTransform = Global.SetNewOrigin(npcInstance.GlobalTransform, spawnPoint.GlobalTransform.Origin);
        npcInstance.Rotation = new Vector3(0, spawnPoint.Rotation.Y,0);

        if (triggerConnections == null) return;
        foreach (var triggerDataPrimary in triggerConnections)
        {
            if (triggerDataPrimary.Obj is not Array { Count: 4 } triggerData) continue;

            var trigger = GetNode(triggerData[0].ToString());
            var signal = triggerData[1].ToString();
            var method = triggerData[2].ToString();
            npcInstance.Connect(signal, new Callable(trigger, method));
        }
    }
    
    public void _on_body_entered(Node body)
    {
        if (body is Player)
        {
            OnActivateTrigger();
        }
    }
}
