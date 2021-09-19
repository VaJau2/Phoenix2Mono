using System;
using Godot;
using Godot.Collections;

public class DialogueTrigger : TriggerBase
{
    [Export] public bool onlyChangeCode;
    [Export] public string npcPath;
    [Export] public bool goToPlayer;
    [Export] public bool changeStateToIdle;
    [Export] public NodePath dialogueStartPointPath;
    [Export] public NodePath afterDialoguePointPath;
    [Export] public float dialogueStartTimer;
    [Export] public string otherDialogueCode;
    
    private Spatial startPoint;
    private Spatial afterDialoguePoint;

    private DialogueMenu dialogueMenu;

    private SavableTimers timers;
    private int step;
    private NpcWithWeapons npc;

    public override void _Ready()
    {
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        timers = GetNode<SavableTimers>("/root/Main/Scene/timers");
        
        
        if (dialogueStartPointPath != null)
        {
            startPoint = GetNode<Spatial>(dialogueStartPointPath);
        }

        if (afterDialoguePointPath != null)
        {
            afterDialoguePoint = GetNode<Spatial>(afterDialoguePointPath);
        }
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        if (npc == null)
        {
            npc = GetNodeOrNull<NpcWithWeapons>(npcPath);
            if (npc == null)
            {
                base._on_activate_trigger();
                return;
            }
        }

        switch (step)
        {
            case 0:
                ChangeNpcCode();
                return;
            case 1:
                SetStartPointAndWait();
                return;
            case 2:
                WaitStartTimer();
                return;
            case 3:
                GoToPlayerAndWait();
                return;
            case 4:
                StartTalking();
                break;
        }
        
        base._on_activate_trigger();
    }

    private void ChangeNpcCode()
    {
        if (otherDialogueCode != null)
        {
            npc.dialogueCode = otherDialogueCode;
        }

        if (npc.state != NPCState.Idle)
        {
            if (changeStateToIdle)
            {
                npc.aggressiveAgainstPlayer = false;
                npc.SetState(NPCState.Idle);
            }
            else
            {
                base._on_activate_trigger();
                return;
            }
        }

        step = 1;
        _on_activate_trigger();
    }

    private async void SetStartPointAndWait()
    {
        if (startPoint != null)
        {
            npc.SetNewStartPos(startPoint.GlobalTransform.origin);
            npc.myStartRot = startPoint.Rotation;
            await ToSignal(npc, nameof(NpcWithWeapons.IsCame));
        }
        
        step = 2;
        _on_activate_trigger();
    }

    private async void WaitStartTimer()
    {
        if (dialogueStartTimer > 0)
        {
            while (timers.CheckTimer(Name + "_timer", dialogueStartTimer))
            {
                await ToSignal(GetTree(), "idle_frame");
            }
        }

        step = 3;
        _on_activate_trigger();
    }

    private async void GoToPlayerAndWait()
    {
        if (goToPlayer)
        {
            npc.SetFollowTarget(Global.Get().player);
            await ToSignal(npc, nameof(NpcWithWeapons.IsCame));
            npc.SetFollowTarget(null);
        }
        
        step = 4;
        _on_activate_trigger();
    }
    
    private void StartTalking()
    {
        if (!onlyChangeCode)
        {
            dialogueMenu.StartTalkingTo(npc);
        }

        if (afterDialoguePoint != null)
        {
            npc.SetNewStartPos(afterDialoguePoint.GlobalTransform.origin);
            npc.myStartRot = afterDialoguePoint.Rotation;
        }
    }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["step"] = step;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            _on_activate_trigger();
        }
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
        
        NpcWithWeapons npc = GetNode<NpcWithWeapons>(npcPath);
        if (IsInstanceValid(npc) && npc.Health > 0)
        {
            _on_activate_trigger();
        }
    }
}
