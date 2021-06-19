using Godot;

public class DialogueTrigger : TriggerBase
{
    [Export] public bool onlyChangeCode;
    [Export] public string npcPath;
    [Export] public bool goToPlayer;
    [Export] public NodePath dialogueStartPointPath;
    [Export] public float dialogueStartTimer;
    [Export] public string otherDialogueCode;
    
    private Spatial startPoint;

    private DialogueMenu dialogueMenu;

    public override void _Ready()
    {
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        
        
        if (dialogueStartPointPath != null)
        {
            startPoint = GetNode<Spatial>(dialogueStartPointPath);
        }
    }

    public override async void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        Pony npc = GetNode<Pony>(npcPath);
        
        if (otherDialogueCode != null)
        {
            npc.dialogueCode = otherDialogueCode;
        }
        
        if (npc.state != NPCState.Idle)
        {
            base._on_activate_trigger();
            return;
        }
        
        if (startPoint != null)
        {
            npc.SetNewStartPos(startPoint.GlobalTransform.origin);
            npc.myStartRot = startPoint.Rotation;
            await ToSignal(npc, nameof(NpcWithWeapons.IsCame));
        }
        if (goToPlayer)
        {
            npc.SetFollowTarget(Global.Get().player);
            await ToSignal(npc, nameof(NpcWithWeapons.IsCame));
            npc.SetFollowTarget(null);
        }
        
        if (!onlyChangeCode)
        {
            if (dialogueStartTimer > 0)
            {
                await Global.Get().ToTimer(dialogueStartTimer);
            }
            
            dialogueMenu.StartTalkingTo(npc);
        }

        base._on_activate_trigger();
    }

    public override void SetActive(bool active)
    {
        _on_activate_trigger();
        base.SetActive(active);
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        
        Pony npc = GetNode<Pony>(npcPath);
        if (npc.Health > 0)
        {
            _on_activate_trigger();
        }
    }
}
