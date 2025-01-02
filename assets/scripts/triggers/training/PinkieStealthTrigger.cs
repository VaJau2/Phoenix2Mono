using Godot;
using Godot.Collections;

public class PinkieStealthTrigger : TrainingTriggerWithButton
{
    private const string WIN_DIALOGUE = "win";
    private const string LOSE_PONY_DIALOGUE = "lose_pony";
    private const string LOSE_UNICORN_DIALOGUE = "lose_unicorn";
    
    [Export] private Array<NodePath> roboEyesPaths;
    [Export] private Array<NodePath> patrolPointParentsPaths;
    [Export] public AudioStream beepSound;
    [Export] private NodePath enterDoorPath;
    [Export] private NodePath bagDoorPath;
    [Export] private NodePath bagPath;
    [Export] private string itemInBag;
    [Export] private NodePath assistantPiePath;
    [Export] private NodePath changeTaskPath;
    
    private Array<Spatial> patrolPointParents = [];
    private Array<NPC> roboEyes = [];
    private FurnDoor enterDoor;
    private FurnDoor bagDoor;
    private BagChest bag;
    private NPC assistantPie;
    private TriggerBase changeTaskTrigger;
    private EnemiesManager enemiesManager;
    
    private bool isRobotsActive;
    
    public override async void _Ready()
    {
        base._Ready();
        
        assistantPie = GetNode<NPC>(assistantPiePath);
        changeTaskTrigger = GetNodeOrNull<TriggerBase>(changeTaskPath);
        enemiesManager = GetNode<EnemiesManager>("/root/Main/Scene/npc");
        
        audi = GetNode<AudioStreamPlayer3D>(audiPath);
        audi.Stream = beepSound;

        enterDoor = GetNode<FurnDoor>(enterDoorPath);
        bagDoor = GetNode<FurnDoor>(bagDoorPath);
        bag = GetNodeOrNull<BagChest>(bagPath);
        bag?.ChestHandler.AddNewItem(itemInBag);
        
        foreach (NodePath roboEyePath in roboEyesPaths)
        {
            var roboEye = GetNode<NPC>(roboEyePath);
            roboEyes.Add(roboEye);
            roboEye.Connect(nameof(NPC.FoundEnemy), this, nameof(_on_found_enemy));
        }

        MakeRoboEyesActive(false);
        
        foreach (NodePath patrolParentPath in patrolPointParentsPaths)
        {
            patrolPointParents.Add(GetNode<Spatial>(patrolParentPath));
        }

        await ToSignal(GetTree(), "idle_frame");

        if (bag != null)
        {
            bag.ChestHandler.TakeItemEvent += _on_take_item_from_bag;
        }
    }

    public override void _ExitTree()
    {
        if (bag != null)
        {
            bag.ChestHandler.TakeItemEvent -= _on_take_item_from_bag;
        }
    }

    protected override void PressButton()
    {
        MakeRoboEyesActive(true);

        bagDoor.myKey = "";
        enterDoor.myKey = "";

        checkButton = false;
    }

    public void _on_found_enemy()
    {
        if (!isRobotsActive) return;
        
        MakeRoboEyesActive(false);
        ChangeRobotsSubtitlesCode("lost");
        
        audi.Stream = beepSound;
        audi.Play();
        
        var loseDialogue = Global.Get().playerRace == Race.Unicorn 
            ? LOSE_UNICORN_DIALOGUE 
            : LOSE_PONY_DIALOGUE;
        
        assistantPie.dialogueCode = loseDialogue;
        bagDoor.myKey = "closed";
        checkButton = true;
    }

    public void _on_take_item_from_bag(string itemCode)
    {
        if (itemCode != itemInBag) return;
        if (trainingIsDone) return;
        
        MakeRoboEyesActive(false);
        ChangeRobotsSubtitlesCode("won");
        
        audi.Stream = beepSound;
        audi.Play();
        assistantPie.dialogueCode = WIN_DIALOGUE;
        
        changeTaskTrigger?.SetActive(true);
        trainingIsDone = true;
        SetActive(false);
    }

    private void MakeRoboEyesActive(bool value)
    {
        foreach (var roboEye in roboEyes)
        {
            if (value)
            {
                if (!enemiesManager.enemies.Contains(roboEye))
                {
                    enemiesManager.enemies.Add(roboEye);
                }
                
                if (roboEye.Health <= 0)
                {
                    roboEye.GetNode<RoboEyeBody>("body").Resurrect();    
                }
            }
            else
            {
                if (enemiesManager.enemies.Contains(roboEye))
                {
                    enemiesManager.enemies.Remove(roboEye);
                }
            }
            
            roboEye.SetState(value ? SetStateEnum.Idle : SetStateEnum.Disabled);
            roboEye.relation = value ? Relation.Enemy : Relation.Friend;
            roboEye.ignoreDamager = !value;
        }

        isRobotsActive = value;
    }

    private void ChangeRobotsSubtitlesCode(string code)
    {
        foreach (var roboEye in roboEyes)
        {
            roboEye.subtitlesCode = code;
        }
    }
    
    public override Dictionary GetSaveData()
    {
        var data = base.GetSaveData();
        data["robotsActive"] = isRobotsActive;
        return data;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        isRobotsActive = (bool) data["robotsActive"];
        
        foreach (var roboEye in roboEyes)
        {
            if (isRobotsActive)
            {
                if (!enemiesManager.enemies.Contains(roboEye))
                {
                    enemiesManager.enemies.Add(roboEye);
                }
            }
            else
            {
                if (enemiesManager.enemies.Contains(roboEye))
                {
                    enemiesManager.enemies.Remove(roboEye);
                }
            }
        }
    }
}
