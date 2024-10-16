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
    [Export] public string newEyesTexture;
    [Export] public string newAnimation;
    [Export] public int newWalkSpeed = -1;
    [Export] public int newRunSpeed = -1;
    [Export] public NodePath newIdlePointPath;
    [Export] public Relation newRelation = Relation.Friend;
    [Export] public string newWeaponCode;
    [Export] public bool ignoreDamager;
    [Export] private Mortality newMortality;

    private enum Mortality
    {
        DontChange,
        Mortal,
        Imortal
    }
    
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

        npc.IsImmortal = newMortality switch
        {
            Mortality.Mortal => false,
            Mortality.Imortal => true,
            _ => npc.IsImmortal
        };

        ChangeObjectsVisible(ref showObjects, true);
        ChangeObjectsVisible(ref hideObjects, false);
        
        if (!string.IsNullOrEmpty(newEyesTexture))
        {
            npc.GetNode<NPCFace>("Armature/Skeleton/Body")?
                .ChangeEyesVariant(newEyesTexture);
        }
    
        if (!string.IsNullOrEmpty(newAnimation))
        {
            var body = npc.GetNodeOrNull<PonyBody>("body");
            if (body != null)
            {
                body.IdleAnim = newAnimation;
            }
        }

        
        if (newWalkSpeed != -1)
        {
            npc.MovingController.BaseSpeed = newWalkSpeed;
        }

        if (newRunSpeed != -1 && npc.MovingController is NavigationMovingController navigation)
        {
            navigation.RunSpeed = newRunSpeed;
        }
        
        if (!string.IsNullOrEmpty(followPath))
        {
            Character followTarget = GetNode<Character>(followPath);
            npc.SetFollowTarget(followTarget);
        }
        else
        {
            npc.SetFollowTarget(null);
        }

        if (newIdlePoint != null)
        {
            npc.SetNewStartPos(newIdlePoint.GlobalTransform.origin);
            npc.myStartRot = newIdlePoint.Rotation;
        }

        if (newWeaponCode != null)
        {
            npc.Weapons?.LoadWeapon(newWeaponCode);
        }

        base._on_activate_trigger();
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
            _on_activate_trigger();
        }
    }
}
