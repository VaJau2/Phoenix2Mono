using Godot;

//меняет статы непися, такие как:
//  - видимые/невидимые объекты внутри нпс
//  - idleanim
//  - скорость ходьбы и бега (если нпц понь)
class ChangeNPCStatesTrigger: ActivateOtherTrigger
{
    [Export] public NodePath npcPath;
    [Export] public string[] showObjects;
    [Export] public string[] hideObjects;
    [Export] public string newAnimation;
    [Export] public int newWalkSpeed = -1;
    [Export] public int newRunSpeed = -1;

    private NPC npc;

    public override void _Ready()
    {
        base._Ready();
        if (npcPath != null)
        {
            npc = GetNode<NPC>(npcPath);
        }
    }

    private void ChangeObjectsVisible(ref string[] objects, bool active)
    {
        foreach (var showObjectPath in objects)
        {
            var showobject = npc.GetNode<Spatial>(showObjectPath);
            if (showobject != null)
            {
                showobject.Visible = active;
            }
        }
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive || npc == null) return;

        ChangeObjectsVisible(ref showObjects, true);
        ChangeObjectsVisible(ref hideObjects, false);
        if (newAnimation != null)
        {
            npc.IdleAnim = newAnimation;
        }

        if (newWalkSpeed != -1)
        {
            npc.WalkSpeed = newWalkSpeed;
        }

        if (npc is Pony pony && newRunSpeed != -1)
        {
            pony.RunSpeed = newRunSpeed;
        }

        base._on_activate_trigger();
    }
}
