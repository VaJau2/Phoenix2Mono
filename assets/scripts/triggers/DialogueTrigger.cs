using System;
using Godot;
using Godot.Collections;

public class DialogueTrigger : TriggerBase
{
    [Export] public bool onlyChangeCode;
    [Export] public string npcPath;
    [Export] public bool goToPlayer;
    [Export] private bool waitPlayer;
    [Export] public bool changeStateToIdle;
    [Export] public NodePath dialogueStartPointPath;
    [Export] public NodePath afterDialoguePointPath;
    [Export] public float dialogueStartTimer;
    [Export] public string otherDialogueCode;
    [Export] private string otherSubtitleCode;

    private bool isPlayerHere;
    private bool isNPCHere;
    
    private Spatial startPoint;
    private Spatial afterDialoguePoint;

    private DialogueMenu dialogueMenu;
    private Subtitles subtitles;

    private float tempTimer;
    private int step;
    private NPC npc;

    public override void _Ready()
    {
        SetProcess(false);
        dialogueMenu = GetNode<DialogueMenu>("/root/Main/Scene/canvas/DialogueMenu/Menu");
        subtitles = GetNode<Subtitles>("/root/Main/Scene/canvas/subtitles");

        if (dialogueStartPointPath != null)
        {
            startPoint = GetNode<Spatial>(dialogueStartPointPath);
        }

        if (afterDialoguePointPath != null)
        {
            afterDialoguePoint = GetNode<Spatial>(afterDialoguePointPath);
        }
    }

    public override void _Process(float delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= delta;
            return;
        }

        SetProcess(false);
        _on_activate_trigger();
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;

        if (npc == null)
        {
            npc = GetNodeOrNull<NPC>(npcPath);
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
        npc.dialogueCode = otherDialogueCode;
        npc.subtitlesCode = otherSubtitleCode;

        npc.MayChangeState = true;

        if (npc.GetState() != SetStateEnum.Idle)
        {
            if (changeStateToIdle)
            {
                npc.aggressiveAgainstPlayer = false;
                npc.SetState(SetStateEnum.Idle);
                npc.SeekArea._on_seekArea_body_exited(Global.Get().player);
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
            npc.SetFollowTarget(null);
            npc.SetNewStartPos(startPoint.GlobalTransform.origin);
            npc.myStartRot = startPoint.Rotation;
            await ToSignal(npc, nameof(NPC.IsCame));
        }
        
        step = 2;
        _on_activate_trigger();
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
            await ToSignal(npc, nameof(NPC.IsCame));
            npc.SetFollowTarget(null);

            var player = Global.Get().player;
            if (player.IsTalking)
            {
                await ToSignal(dialogueMenu, nameof(DialogueMenu.FinishTalking));
                await ToSignal(GetTree(), "idle_frame");
            }
        }
        
        step = 4;
        _on_activate_trigger();
    }
    
    private void StartTalking()
    {
        if (!onlyChangeCode)
        {
            if (!string.IsNullOrEmpty(npc.dialogueCode))
            {
                dialogueMenu.StartTalkingTo(npc);
            }
            else if (!string.IsNullOrEmpty(npc.subtitlesCode))
            {
                subtitles.SetTalker(npc)
                    .LoadSubtitlesFile(npc.subtitlesCode)
                    .StartAnimatingText();
            }
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
        saveData["tempTimer"] = tempTimer;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        
        if (data.Contains("tempTimer"))
        {
            tempTimer = Convert.ToSingle(data["tempTimer"]);
        }
        
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            _on_activate_trigger();
        }
    }

    public override void SetActive(bool active)
    {
        if (active || !goToPlayer)
        {
            _on_activate_trigger();
        }
            
        base.SetActive(active);
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        
        if (waitPlayer) Waiting(body);
        else Default(body);
    }

    public void _on_body_exited(Node body)
    {
        if (!IsActive) return;
        if (!waitPlayer) return;

        switch (body)
        {
            case NPC enteredNpc:
            {
                if (enteredNpc == npc)
                {
                    isNPCHere = false;
                }
                break;
            }

            case Player:
            {
                isPlayerHere = false;
                break;
            }
        }
    }
    
    private void Waiting(Node body)
    {
        switch (body)
        {
            case NPC enteredNpc:
            {
                var localNpc = GetNodeOrNull<NPC>(npcPath);
                if (enteredNpc != localNpc) break;
                isNPCHere = true;
                if (isPlayerHere) _on_activate_trigger();
                break;
            }
            
            case Player:
            {
                isPlayerHere = true;
                if (isNPCHere) _on_activate_trigger();
                break;
            }
        }
    }

    private void Default(Node body)
    {
        if (body is not Player) return;
        
        var localNpc = GetNodeOrNull<NPC>(npcPath);
        if (IsInstanceValid(localNpc) && localNpc.Health > 0)
        {
            _on_activate_trigger();
        }
    }
}
