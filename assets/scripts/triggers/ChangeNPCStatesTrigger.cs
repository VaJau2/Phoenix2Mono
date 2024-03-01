using Godot;

//меняет статы непися, такие как:
//  - видимые/невидимые объекты внутри нпс
//  - idleanim
//  - скорость ходьбы и бега (если нпц понь)
partial class ChangeNPCStatesTrigger: ActivateOtherTrigger
{
    [Export] public string npcPath;
    [Export] public string followPath;
    [Export] public string[] showObjects;
    [Export] public string[] hideObjects;
    [Export] public string newAnimation;
    [Export] public int newWalkSpeed = -1;
    [Export] public int newRunSpeed = -1;
    [Export] public NodePath newIdlePointPath;
    [Export] public Relation newRelation = Relation.Friend;
    [Export] public string newWeaponCode;
    [Export] public bool ignoreDamager;
    [Export] public bool stayInPoint;

    private NPC npc;
    private Node3D newIdlePoint;

    public override void _Ready()
    {
        base._Ready();
        
        if (newIdlePointPath != null)
        {
            newIdlePoint = GetNode<Node3D>(newIdlePointPath);
        }
    }

    private void ChangeObjectsVisible(ref string[] objects, bool active)
    {
        if (objects == null) return;
        
        foreach (var showObjectPath in objects)
        {
            npc.SetObjectActive(showObjectPath, active);
        }
    }
    
    
    public override void SetActive(bool newActive)
    {
        OnActivateTrigger();
        base.SetActive(newActive);
    }
    
    public override void OnActivateTrigger()
    {
        if (!IsActive) return;
        
        if (npc == null && npcPath != null)
        {
            npc = GetNode<NPC>(npcPath);
        }
        
        if (!IsInstanceValid(npc) || npc == null)
        {
            base.OnActivateTrigger();
            return;
        }

        npc.relation = newRelation;
        npc.ignoreDamager = ignoreDamager;
        ChangeObjectsVisible(ref showObjects, true);
        ChangeObjectsVisible(ref hideObjects, false);
        if (!string.IsNullOrEmpty(newAnimation))
        {
            npc.IdleAnim = newAnimation;
        }

        if (newWalkSpeed != -1)
        {
            npc.WalkSpeed = newWalkSpeed;
        }

        if (npc is Pony pony && newRunSpeed != -1)
        {
            pony.stayInPoint = stayInPoint;
            pony.RunSpeed = newRunSpeed;
        }
        
        if (npc is NpcWithWeapons npcWithWeapons)
        {
            if (!string.IsNullOrEmpty(followPath))
            {
                Character followTarget = GetNode<Character>(followPath);
                npcWithWeapons.SetFollowTarget(followTarget);
            }
            else
            {
                npcWithWeapons.SetFollowTarget(null);
            }

            if (newIdlePoint != null)
            {
                npcWithWeapons.SetNewStartPos(newIdlePoint.GlobalTransform.Origin);
                npc.myStartRot = newIdlePoint.Rotation;
            }

            if (newWeaponCode != null)
            {
                npcWithWeapons.weaponCode = newWeaponCode;
                npcWithWeapons.weapons.LoadWeapon(npcWithWeapons, newWeaponCode);
            }
        }

        base.OnActivateTrigger();
    }
    
    
    public override void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        
        if (npcPath != null)
        {
            npc = GetNode<NPC>(npcPath);
        }
        
        if (npc.Health > 0)
        {
            OnActivateTrigger();
        }
    }
}
