using Godot;
using Godot.Collections;

public class DashTrainingTrigger : TriggerBase
{
    [Export] public Array<NodePath> TargetPaths;
    [Export] public string HintCode = "clickButton";
    [Export] public float StartTime = 1f;
    [Export] public AudioStream beepSound;
    [Export] public NodePath audiPath;
    [Export] public NodePath roboDashPath;
    [Export] public string loseDialogue = "lose1";
    [Export] public string win1Dialogue = "win1";
    [Export] public string win2Dialogue = "win2";
    private AudioStreamPlayer3D audi;
    private MrHandy roboDash;
    private Array<Target> targets = new Array<Target>();
    private bool checkButton;
    private int tempTargetsCount;
    private bool playerHere;
    
    private static Player player => Global.Get().player;
    
    public override void _Ready()
    {
        base._Ready();
        SetProcess(false);
        
        audi = GetNode<AudioStreamPlayer3D>(audiPath);
        audi.Stream = beepSound;
        roboDash = GetNode<MrHandy>(roboDashPath);
        
        if (TargetPaths == null) return;
        foreach (var targetPath in TargetPaths)
        {
            targets.Add(GetNode<Target>(targetPath));
        }
    }
    
    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        if (!checkButton)
        {
            checkButton = true;
            return;
        }
        
        base._on_activate_trigger();
    }

    public void _on_body_entered(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        playerHere = true;
        if (!checkButton) return;
        SetProcess(true);
    }
    
    public void _on_body_exited(Node body)
    {
        if (!IsActive) return;
        if (!(body is Player)) return;
        playerHere = false;
        if (!checkButton) return;
        player?.Camera.HideHint();
        SetProcess(false);
    }

    public void _on_target_die()
    {
        tempTargetsCount++;
    }

    public override void _Process(float delta)
    {
        player.Camera.ShowHint(HintCode, false);

        if (!Input.IsActionJustPressed("use")) return;
        tempTargetsCount = 0;
        audi.Play();
        
        foreach (var target in targets)
        {
            target.SetHealth(Target.TARGET_HEALTH);
        }
        player?.Camera.HideHint();
        SetProcess(false);
        checkButton = false;
        CountTimer();
    }

    private bool timerIsSmall => (StartTime == 1f);
    
    private async void CountTimer()
    {
        await Global.Get().ToTimer(StartTime);
        foreach (var target in targets)
        {
            target.Off();
        }
        audi.Play();
        
        if (tempTargetsCount == targets.Count)
        {
            roboDash.dialogueCode = (timerIsSmall) ? win1Dialogue : win2Dialogue;
        }
        else
        {
            if (timerIsSmall)
            {
                roboDash.dialogueCode = loseDialogue;
            }
            
            checkButton = true;
            
            if (playerHere)
            {
                _on_body_entered(player);
            }
        }
    }
}
