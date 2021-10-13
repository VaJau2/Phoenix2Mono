using Godot;
using Godot.Collections;

public class DashTrainingTrigger : TrainingTriggerWithButton
{
    [Export] public Array<NodePath> TargetPaths;
    [Export] public float StartTime = 1f;
    [Export] public AudioStream beepSound;
    [Export] public NodePath roboDashPath;
    [Export] public string loseDialogue = "lose1";
    [Export] public string win1Dialogue = "win1";
    [Export] public string win2Dialogue = "win2";
   
    private MrHandy roboDash;
    private Array<Target> targets = new Array<Target>();
    private int tempTargetsCount;
    
    public override void _Ready()
    {
        base._Ready();
        
        roboDash = GetNode<MrHandy>(roboDashPath);
        
        if (TargetPaths == null) return;
        foreach (var targetPath in TargetPaths)
        {
            targets.Add(GetNode<Target>(targetPath));
        }
    }
    
    public void _on_target_die()
    {
        tempTargetsCount++;
    }

    protected override void PressButton()
    {
        tempTargetsCount = 0;

        foreach (var target in targets)
        {
            target.SetHealth(Target.TARGET_HEALTH);
        }
        
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
