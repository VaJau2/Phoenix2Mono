using Godot;

public class DialogueTrigger : TriggerBase
{
    [Export] public NodePath npcPath;
    [Export] public NodePath dialogueStartPointPath;
    [Export] public NodePath dialogueEndPointPath;
    [Export] public string otherDialogueCode;

    private Pony npc;
    private Vector3 startPoint = Vector3.Zero;
    private Vector3 endPoint = Vector3.Zero;
    
    private DialogueMenu dialogueMenu;

    public override void _Ready()
    {
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        
        npc = GetNode<Pony>(npcPath);
        if (dialogueStartPointPath != null)
        {
            startPoint = GetNode<Spatial>(dialogueStartPointPath).GlobalTransform.origin;
        }
        if (dialogueEndPointPath != null)
        {
            endPoint = GetNode<Spatial>(dialogueEndPointPath).GlobalTransform.origin;
        }
    }

    public override async void _on_activate_trigger()
    {
        if (startPoint != Vector3.Zero)
        {
            npc.SetNewStartPos(startPoint);
            await ToSignal(npc, nameof(NpcWithWeapons.IsCame));
        }

        if (otherDialogueCode != null)
        {
            npc.dialogueCode = otherDialogueCode;
        }

        dialogueMenu.StartTalkingTo(npc);

        if (endPoint != Vector3.Zero)
        {
            await ToSignal(dialogueMenu, nameof(DialogueMenu.FinishTalking));
            npc.SetNewStartPos(endPoint);
        }
        
        base._on_activate_trigger();
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
