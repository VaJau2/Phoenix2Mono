using Godot;

//меняет статы непися, такие как:
//  - видимые/невидимые объекты внутри нпс
//  - idleanim
//  - скорость ходьбы и бега (если нпц понь)
class ChangeNPCStatesTrigger: ActivateOtherTrigger
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
    private Spatial newIdlePoint;

    public override void _Ready()
    {
        base._Ready();
        
        if (newIdlePointPath != null)
        {
            newIdlePoint = GetNode<Spatial>(newIdlePointPath);
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
        _on_activate_trigger();
        base.SetActive(newActive);
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        if (npc == null && npcPath != null)
        {
            npc = GetNode<NPC>(npcPath);
        }
        
        if (!IsInstanceValid(npc) || npc == null)
        {
            base._on_activate_trigger();
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
            if (followPath != null)
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
                npcWithWeapons.SetNewStartPos(newIdlePoint.GlobalTransform.origin);
                npc.myStartRot = newIdlePoint.Rotation;
            }

            if (newWeaponCode != null)
            {
                npcWithWeapons.weaponCode = newWeaponCode;
                npcWithWeapons.weapons.LoadWeapon(npcWithWeapons, newWeaponCode);
            }
        }

        base._on_activate_trigger();
    }
    
    
    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        
        if (npcPath != null)
        {
            npc = GetNode<NPC>(npcPath);
        }
        
        if (npc.Health > 0)
        {
            _on_activate_trigger();
        }
    }
}
