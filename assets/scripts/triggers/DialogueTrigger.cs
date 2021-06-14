using Godot;

public class DialogueTrigger : TriggerBase
{
    [Export] public bool onlyChangeCode;
    [Export] public NodePath npcPath;
    [Export] public NodePath dialogueStartPointPath;
    [Export] public float dialogueStartTimer;
    [Export] public NodePath dialogueEndPointPath;
    [Export] public string otherDialogueCode;

    private Pony npc;
    private Spatial startPoint;
    private Spatial endPoint;
    
    private DialogueMenu dialogueMenu;

    public override void _Ready()
    {
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        
        npc = GetNode<Pony>(npcPath);
        if (dialogueStartPointPath != null)
        {
            startPoint = GetNode<Spatial>(dialogueStartPointPath);
        }
        if (dialogueEndPointPath != null)
        {
            endPoint = GetNode<Spatial>(dialogueEndPointPath);
        }
    }

    public override async void _on_activate_trigger()
    {
        if (!IsActive) return;
        
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
        
        if (!onlyChangeCode)
        {
            if (dialogueStartTimer > 0)
            {
                await Global.Get().ToTimer(dialogueStartTimer);
            }
            
            dialogueMenu.StartTalkingTo(npc);
        }

        if (endPoint != null)
        {
            await ToSignal(dialogueMenu, nameof(DialogueMenu.FinishTalking));
            npc.SetNewStartPos(endPoint.GlobalTransform.origin);
            npc.myStartRot = endPoint.Rotation;
        }
        
        base._on_activate_trigger();
    }

    public override void SetActive(bool active)
    {
        base.SetActive(active);
        _on_activate_trigger();
    }

    public async void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        if (npc.Health > 0)
        {
            _on_activate_trigger();
        }
    }
}
