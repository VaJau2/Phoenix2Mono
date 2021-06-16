using Godot;
using Godot.Collections;

public class SpawnerNpcTrigger: TriggerBase
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
    }

    public override void SetActive(bool newActive)
    {
        base.SetActive(newActive);
        _on_activate_trigger();
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
        }
       
        
        base._on_activate_trigger();
    }
}
