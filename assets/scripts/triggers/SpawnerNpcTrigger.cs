using Godot;
using Godot.Collections;

public class SpawnerNpcTrigger: ActivateOtherTrigger
{
    [Export] private PackedScene npcPrefab;
    [Export] private NodePath spawnPointPath;
    [Export] private float spawnDelay;
    [Export] private NodePath movePointPath;
    [Export] private bool run;

    [Export] private int StartHealth = 100;
    [Export] private string weaponCode;
    [Export] private Relation relation;
    [Export]
    private Array<string> itemCodes = new Array<string>();
    [Export]
    private Dictionary<string, int> ammoCount = new Dictionary<string, int>();

    [Export] private Array<NodePath> triggersWhenDeadPaths;
    private Array<TriggerBase> triggersWhenDead = new Array<TriggerBase>();

    private Spatial spawnPoint;
    private Spatial movePoint;

    public override void _Ready()
    {
        if (spawnPointPath != null)
        {
            spawnPoint = GetNode<Spatial>(spawnPointPath);
        }
        if (movePointPath != null)
        {
            movePoint = GetNode<Spatial>(movePointPath);
        }

        if (triggersWhenDeadPaths != null)
        {
            foreach (var triggerPath in triggersWhenDeadPaths)
            {
                triggersWhenDead.Add(GetNode<TriggerBase>(triggerPath));
            }
        }
        base._Ready();
    }

    public override void SetActive(bool newActive)
    {
        _on_activate_trigger();
        base.SetActive(newActive);
    }

    public override async void _on_activate_trigger()
    {
        if (!IsActive) return;

        if (spawnDelay > 0)
        {
            await Global.Get().ToTimer(spawnDelay);
        }
        
        if (npcPrefab.Instance() is NpcWithWeapons npcInstance)
        {
            npcInstance.Name = "Created_" + npcInstance.Name;
            npcInstance.StartHealth = StartHealth;
            npcInstance.weaponCode = weaponCode;
            npcInstance.relation = relation;
            npcInstance.itemCodes = itemCodes;
            npcInstance.ammoCount = ammoCount;
            GetNode<Node>("/root/Main/Scene/npc").AddChild(npcInstance);
            npcInstance.GlobalTransform = Global.setNewOrigin(npcInstance.GlobalTransform, spawnPoint.GlobalTransform.origin);
            npcInstance.Rotation = new Vector3(0, spawnPoint.Rotation.y,0);
            if (movePoint != null)
            {
                npcInstance.SetNewStartPos(movePoint.GlobalTransform.origin, run);
                npcInstance.myStartRot = new Vector3(0, movePoint.Rotation.y, 0);
            }
            else
            {
                npcInstance.SetNewStartPos(spawnPoint.GlobalTransform.origin, run);
                npcInstance.myStartRot = new Vector3(0, spawnPoint.Rotation.y, 0);
            }
            foreach (var trigger in triggersWhenDead)
            {
                var binds = new Array {true};
                npcInstance.Connect(nameof(Character.Die), trigger, nameof(TriggerBase.SetActive), binds);
            }
        }
       
        base._on_activate_trigger();
    }

    public void _on_body_entered(Node body)
    {
        if (body is Player)
        {
            _on_activate_trigger();
        }
    }
}
