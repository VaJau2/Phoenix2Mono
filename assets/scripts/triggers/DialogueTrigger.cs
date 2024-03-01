using System;
using Godot;
using Godot.Collections;

public partial class DialogueTrigger : TriggerBase
{
    [Export] public bool onlyChangeCode;
    [Export] public string npcPath;
    [Export] public bool goToPlayer;
    [Export] public bool changeStateToIdle;
    [Export] public NodePath dialogueStartPointPath;
    [Export] public NodePath afterDialoguePointPath;
    [Export] public float dialogueStartTimer;
    [Export] public string otherDialogueCode;
    
    private Node3D startPoint;
    private Node3D afterDialoguePoint;

    private DialogueMenu dialogueMenu;

    private double tempTimer;
    private int step;
    private NpcWithWeapons npc;

    public override void _Ready()
    {
        SetProcess(false);
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");

        if (dialogueStartPointPath != null)
        {
            startPoint = GetNode<Node3D>(dialogueStartPointPath);
        }

        if (afterDialoguePointPath != null)
        {
            afterDialoguePoint = GetNode<Node3D>(afterDialoguePointPath);
        }
    }

    public override void _Process(double delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= delta;
            return;
        }

        SetProcess(false);
        OnActivateTrigger();
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;

        if (npc == null)
        {
            npc = GetNodeOrNull<NpcWithWeapons>(npcPath);
            if (npc == null)
            {
                base.OnActivateTrigger();
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
        
        base.OnActivateTrigger();
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
                npc.seekArea._on_seekArea_body_exited(Global.Get().player);
            }
            else
            {
                base.OnActivateTrigger();
                return;
            }
        }

        step = 1;
        OnActivateTrigger();
    }

    private async void SetStartPointAndWait()
    {
        if (startPoint != null)
        {
            npc.SetNewStartPos(startPoint.GlobalTransform.Origin);
            npc.myStartRot = startPoint.Rotation;
            await ToSignal(npc, nameof(NpcWithWeapons.IsCame));
        }
        
        step = 2;
        OnActivateTrigger();
    }

    private void WaitStartTimer()
    {
        tempTimer = dialogueStartTimer;
        step = 3;
        SetProcess(true);
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
        OnActivateTrigger();
    }
    
    private void StartTalking()
    {
        if (!onlyChangeCode && !string.IsNullOrEmpty(npc.dialogueCode))
        {
            dialogueMenu.StartTalkingTo(npc);
        }

        if (afterDialoguePoint != null)
        {
            npc.SetNewStartPos(afterDialoguePoint.GlobalTransform.Origin);
            npc.myStartRot = afterDialoguePoint.Rotation;
        }
    }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["step"] = step;
        saveData["tempTimer"] = tempTimer;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        
        if (data.TryGetValue("tempTimer", out var timerValue))
        {
            tempTimer = timerValue.AsSingle();
        }
        
        step = data["step"].AsInt16();
        if (step > 0)
        {
            OnActivateTrigger();
        }
    }

    public override void SetActive(bool active)
    {
        OnActivateTrigger();
        base.SetActive(active);
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        
        NpcWithWeapons npc = GetNodeOrNull<NpcWithWeapons>(npcPath);
        if (IsInstanceValid(npc) && npc.Health > 0)
        {
            OnActivateTrigger();
        }
    }
}
