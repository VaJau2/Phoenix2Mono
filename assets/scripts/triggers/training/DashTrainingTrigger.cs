using Godot;
using Godot.Collections;

public class DashTrainingTrigger : TrainingTriggerWithButton
{
    [Export] public Array<NodePath> TargetPaths;
    [Export] public float StartTime = 1f;
    [Export] public AudioStream beepSound;
    [Export] public NodePath assistantDashPath;
    [Export] public string loseDialogue = "lose1";
    [Export] public string win1Dialogue = "win1";
    [Export] public string win2Dialogue = "win2";
   
    private MrHandy assistantDash;
    private Array<Target> targets = new Array<Target>();
    private int tempTargetsCount;
    private bool isCounting;
    
    public override void _Ready()
    {
        base._Ready();
        
        assistantDash = GetNode<MrHandy>(assistantDashPath);
        
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
        isCounting = true;
        await Global.Get().ToTimer(StartTime);
        isCounting = false;
        foreach (var target in targets)
        {
            target.Off();
        }
        audi.Play();

        if (!trainingIsDone)
        {
            if (tempTargetsCount == targets.Count)
            {
                assistantDash.dialogueCode = (timerIsSmall) ? win1Dialogue : win2Dialogue;
                trainingIsDone = true;
                SetActive(false);
            }
            else
            {
                if (timerIsSmall)
                {
                    assistantDash.dialogueCode = loseDialogue;
                }
            }
        }
        
        checkButton = true;
    }
    
    public override Dictionary GetSaveData()
    {
        var data = base.GetSaveData();
        data["timer"] = StartTime;
        data["isCounting"] = isCounting;

        return data;
    }
    
    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        StartTime = (float) data["timer"];
        
        //при сохранении не во время включенного тира его таймер не сохраняется, и он загружается выключенным
        //при этом нужно вернуть ему возможность включения
        if ((bool) data["isCounting"])
        {
            checkButton = true;
        }
    }
}
