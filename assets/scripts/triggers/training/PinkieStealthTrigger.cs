using Godot;
using Godot.Collections;

public class PinkieStealthTrigger : TrainingTriggerWithButton
{
    [Export] private Array<NodePath> roboEyesPaths;
    [Export] private Array<NodePath> patrolPointParentsPaths;
    [Export] public AudioStream beepSound;
    [Export] private NodePath enterDoorPath;
    [Export] private NodePath bagDoorPath;
    [Export] private NodePath bagPath;
    [Export] private string itemInBag;
    [Export] private NodePath assistantPiePath;
    [Export] private string winDialogue;
    [Export] private string loseDialogue;
    
    private Array<Spatial> patrolPointParents = [];
    private Array<NPC> roboEyes = [];
    private FurnDoor enterDoor;
    private FurnDoor bagDoor;
    private BagChest bag;
    private NPC assistantPie;
    
    private bool isRobotsActive;
    
    public override async void _Ready()
    {
        base._Ready();
        
        assistantPie = GetNode<NPC>(assistantPiePath);
        
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
        
        player.Connect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
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
        
        audi.Stream = beepSound;
        audi.Play();
        assistantPie.dialogueCode = loseDialogue;
        bagDoor.myKey = "closed";
        
        checkButton = true;
    }

    public void _on_player_take_item(string itemCode)
    {
        if (itemCode != itemInBag) return;
        if (!isRobotsActive || trainingIsDone) return;
        
        MakeRoboEyesActive(false);
        
        audi.Stream = beepSound;
        audi.Play();
        assistantPie.dialogueCode = winDialogue;
        
        trainingIsDone = true;
        SetActive(false);
    }

    private void MakeRoboEyesActive(bool value)
    {
        foreach (var roboEye in roboEyes)
        {
            roboEye.SetState(value ? SetStateEnum.Idle : SetStateEnum.Disabled);
        }

        isRobotsActive = value;
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
    }
}
