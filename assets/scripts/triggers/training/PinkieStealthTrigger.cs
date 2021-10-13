using Godot;
using Godot.Collections;


public class PinkieStealthTrigger : TrainingTriggerWithButton
{
    [Export] public Array<NodePath> RoboEyeSpawnersPaths;
    [Export] private Array<NodePath> PatrolPointParentsPaths;
    [Export] public AudioStream beepSound;
    [Export] private PackedScene npcPrefab;
    [Export] private PackedScene bagPrefab;
    [Export] private NodePath bagSpawnPath;
    [Export] private string itemInBag;
    [Export] public NodePath roboPinkiePath;
    [Export] private string winDialogue;
    
    private Array<Spatial> patrolPointParents = new Array<Spatial>();
    private Array<Spatial> eyesSpawners = new Array<Spatial>();
    private Array<RoboEye> eyes = new Array<RoboEye>();
    private FurnChest bag;
    private MrHandy roboPinkie;
    
    private void LoadNodePathArray(Array<NodePath> pathArray, ref Array<Spatial> arrayToLoad)
    {
        if (pathArray == null) return;
        foreach (NodePath item in pathArray)
        {
            arrayToLoad.Add(GetNode<Spatial>(item));
        }
    }
    
    public override void _Ready()
    {
        base._Ready();
        
        roboPinkie = GetNode<MrHandy>(roboPinkiePath);
        audi = GetNode<AudioStreamPlayer3D>(audiPath);
        audi.Stream = beepSound;

        LoadNodePathArray(RoboEyeSpawnersPaths, ref eyesSpawners);
        LoadNodePathArray(PatrolPointParentsPaths, ref patrolPointParents);
    }

    protected override void PressButton()
    {
        foreach (var eye in eyes)
        {
            if (IsInstanceValid(eye))
            {
                eye.QueueFree();
            }
        }
        eyes.Clear();
        
        for (int i = 0; i < eyesSpawners.Count; i++)
        {
            Spatial spawner = eyesSpawners[i];
            Spatial patrolParent = patrolPointParents[i];
            if (!(npcPrefab.Instance() is RoboEye eyeInstance)) continue;
            
            eyeInstance.patrolArray = new Array<NodePath>();
            foreach (Spatial child in patrolParent.GetChildren())
            {
                eyeInstance.patrolArray.Add(child.GetPath());
            }
            GetNode<Node>("/root/Main/Scene/npc").AddChild(eyeInstance);
            eyeInstance.GlobalTransform = Global.setNewOrigin(eyeInstance.GlobalTransform, spawner.GlobalTransform.origin);
            eyeInstance.Connect(nameof(RoboEye.FoundEnemy), this, nameof(_on_found_enemy));
            eyes.Add(eyeInstance);
        }

        if (!(bagPrefab.Instance() is FurnChest bagInstance)) return;
        bagInstance.itemCodes.Add(itemInBag);
        GetNode<Node>("/root/Main/Scene/rooms/stels-house").AddChild(bagInstance);
        Spatial bagSpawn = GetNode<Spatial>(bagSpawnPath);
        bagInstance.GlobalTransform = Global.setNewOrigin(bagInstance.GlobalTransform, bagSpawn.GlobalTransform.origin);
        bag = bagInstance;
        
        player.Connect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
    }

    public void _on_found_enemy()
    {
        foreach (var eye in eyes)
        {
            eye.MakeActive(false);
        }
        
        audi.Stream = beepSound;
        audi.Play();
        bag.QueueFree();
        player.Disconnect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        
        checkButton = true;
            
        if (playerHere)
        {
            _on_body_entered(player);
        }
    }

    public void _on_player_take_item(string itemCode)
    {
        if (itemCode != itemInBag) return;
        foreach (var eye in eyes)
        {
            eye.MakeActive(false);
        }
        audi.Stream = beepSound;
        audi.Play();
        roboPinkie.dialogueCode = winDialogue;
    }
}
