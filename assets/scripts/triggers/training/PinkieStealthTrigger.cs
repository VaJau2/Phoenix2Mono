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
    [Export] private string loseDialogue;
    
    private Array<Spatial> patrolPointParents = new Array<Spatial>();
    private Array<Spatial> eyesSpawners = new Array<Spatial>();
    private Array<RoboEye> eyes = new Array<RoboEye>();
    private FurnChest bag;
    private MrHandy roboPinkie;
    private bool connected;
    
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
        
        //спавн робо-глаз
        for (int i = 0; i < eyesSpawners.Count; i++)
        {
            Spatial spawner = eyesSpawners[i];
            Spatial patrolParent = patrolPointParents[i];
            if (!(npcPrefab.Instance() is RoboEye eyeInstance)) continue;
            
            eyeInstance.Name = "Created_" + eyeInstance.Name + "_" + i;
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

        //спавн сумки
        if (!(bagPrefab.Instance() is FurnChest bagInstance)) return;
        bagInstance.Name = "Created_" + bagInstance.Name;
        bagInstance.itemCodes.Add(itemInBag);
        GetNode<Node>("/root/Main/Scene/rooms/stels-house").AddChild(bagInstance);
        Spatial bagSpawn = GetNode<Spatial>(bagSpawnPath);
        bagInstance.GlobalTransform = Global.setNewOrigin(bagInstance.GlobalTransform, bagSpawn.GlobalTransform.origin);
        bag = bagInstance;
        
        player.Connect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        connected = true;
    }

    public void _on_found_enemy()
    {
        if (!connected) return;
        
        foreach (var eye in eyes)
        {
            eye?.MakeActive(false);
        }
        
        audi.Stream = beepSound;
        audi.Play();
        roboPinkie.dialogueCode = loseDialogue;
        bag.QueueFree();
        player.Disconnect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        connected = false;
        
        checkButton = true;
            
        if (playerHere)
        {
            _on_body_entered(player);
        }
    }

    public void _on_player_take_item(string itemCode)
    {
        if (itemCode != itemInBag) return;
        if (trainingIsDone) return;
        
        foreach (var eye in eyes)
        {
            eye?.MakeActive(false);
        }
        audi.Stream = beepSound;
        audi.Play();
        roboPinkie.dialogueCode = winDialogue;
        
        player.Disconnect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        connected = false;
        
        trainingIsDone = true;
    }
    
    public override Dictionary GetSaveData()
    {
        var data = base.GetSaveData();
        
        //сохраняем массив робо-глаз
        Array<string> eyesPath = new Array<string>();
        foreach (var eye in eyes)
        {
            if (IsInstanceValid(eye))
            {
                eyesPath.Add(eye.GetPath().ToString());
            }
        }

        data["eyesPath"] = eyesPath;

        //сохраняем сумку
        if (bag != null && IsInstanceValid(bag))
        {
            data["bagPath"] = bag.GetPath().ToString();
        }
        
        //сохраняем событие
        data["eventConnected"] = connected;

        return data;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);

        //загружаем массив робо-глаз
        if (data["eyesPath"] is Array eyesPath)
        {
            foreach (var eyePath in eyesPath)
            {
                var eye = GetNode<RoboEye>(eyePath.ToString());
                if (eye != null)
                {
                    eyes.Add(eye);
                }
            }
        }

        //загружаем сумку
        if (data.Contains("bagPath"))
        {
            bag = GetNode<FurnChest>(data["bagPath"].ToString());
        }
        
        //загружаем событие
        connected = (bool) data["eventConnected"];
        if (connected)
        {
            player.Connect(nameof(Player.TakeItem), this, nameof(_on_player_take_item));
        }
    }
}
