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
    private Array<RoboEye> roboEyes = [];
    private FurnDoor enterDoor;
    private FurnDoor bagDoor;
    private BagChest bag;
    private MrHandy assistantPie;
    private bool connected;
    
    public override void _Ready()
    {
        base._Ready();
        
        assistantPie = GetNode<MrHandy>(assistantPiePath);
        
        audi = GetNode<AudioStreamPlayer3D>(audiPath);
        audi.Stream = beepSound;

        enterDoor = GetNode<FurnDoor>(enterDoorPath);
        bagDoor = GetNode<FurnDoor>(bagDoorPath);
        bag = GetNodeOrNull<BagChest>(bagPath);
        bag?.ChestHandler.AddNewItem(itemInBag);
        
        foreach (NodePath roboEyePath in roboEyesPaths)
        {
            roboEyes.Add(GetNode<RoboEye>(roboEyePath));
        }
        
        foreach (NodePath patrolParentPath in patrolPointParentsPaths)
        {
            patrolPointParents.Add(GetNode<Spatial>(patrolParentPath));
        }
        
        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");
        
        if (trainingIsDone) return;
        if (!connected) return;

        player.Connect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
    }

    protected override void PressButton()
    {
        SetConnect(true);

        bagDoor.myKey = "";
        enterDoor.myKey = "";

        checkButton = false;
    }

    public void _on_found_enemy()
    {
        if (!connected) return;
        
        SetConnect(false);
        
        audi.Stream = beepSound;
        audi.Play();
        assistantPie.dialogueCode = loseDialogue;
        bagDoor.myKey = "closed";
        
        checkButton = true;
    }

    public void _on_player_take_item(string itemCode)
    {
        if (itemCode != itemInBag) return;
        if (trainingIsDone) return;
        
        SetConnect(false);
        
        audi.Stream = beepSound;
        audi.Play();
        assistantPie.dialogueCode = winDialogue;
        
        trainingIsDone = true;
        SetActive(false);
    }

    private void SetConnect(bool value)
    {
        foreach (var roboEye in roboEyes)
        {
            roboEye.MakeActive(value);
            
            if (value) roboEye.Connect(nameof(RoboEye.FoundEnemy), this, nameof(_on_found_enemy));
            else roboEye.Disconnect(nameof(RoboEye.FoundEnemy), this, nameof(_on_found_enemy));
        }
        
        if (value) player.Connect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        else player.Disconnect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        
        connected = value;
    }
    
    public override Dictionary GetSaveData()
    {
        var data = base.GetSaveData();
        data["eventConnected"] = connected;
        return data;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        connected = (bool) data["eventConnected"];
    }
}
