using Godot;
using Godot.Collections;

public partial class PinkieStealthTrigger : TrainingTriggerWithButton
{
    [Export] public Array<NodePath> RoboEyeSpawnersPaths;
    [Export] private Array<NodePath> PatrolPointParentsPaths;
    [Export] public AudioStream beepSound;
    [Export] private PackedScene npcPrefab;
    [Export] private PackedScene bagPrefab;
    [Export] private NodePath bagSpawnPath;
    [Export] private string itemInBag;
    [Export] public NodePath assistantPiePath;
    [Export] private string winDialogue;
    [Export] private string loseDialogue;
    
    private Array<Node3D> patrolPointParents = new();
    private Array<Node3D> eyesSpawners = new();
    private Array<RoboEye> eyes = new();
    private FurnChest bag;
    private MrHandy assistantPie;
    private bool connected;
    
    private void LoadNodePathArray(Array<NodePath> pathArray, ref Array<Node3D> arrayToLoad)
    {
        if (pathArray == null) return;
        foreach (NodePath item in pathArray)
        {
            arrayToLoad.Add(GetNode<Node3D>(item));
        }
    }
    
    public override void _Ready()
    {
        base._Ready();
        
        assistantPie = GetNode<MrHandy>(assistantPiePath);
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
            Node3D spawner = eyesSpawners[i];
            Node3D patrolParent = patrolPointParents[i];
            if (npcPrefab.Instantiate<RoboEye>() is not { } eyeInstance) continue;
            
            eyeInstance.Name = "Created_" + eyeInstance.Name + "_" + i;
            eyeInstance.patrolArray = new Array<NodePath>();
            foreach (var node in patrolParent.GetChildren())
            {
                var child = (Node3D)node;
                eyeInstance.patrolArray.Add(child.GetPath());
            }
            GetNode<Node>("/root/Main/Scene/npc").AddChild(eyeInstance);
            eyeInstance.GlobalTransform = Global.SetNewOrigin(eyeInstance.GlobalTransform, spawner.GlobalTransform.Origin);
            eyeInstance.FoundEnemy += OnFoundEnemy;
            
            eyes.Add(eyeInstance);
        }
        
        SpawnBag();
        
        player.TakeItem += OnPlayerTakeItem;
        connected = true;
    }

    private void SpawnBag()
    {
        if (bagPrefab.Instantiate<FurnChest>() is not { } bagInstance) return;
        bagInstance.Name = "Created_" + bagInstance.Name;
        GetNode<Node>("/root/Main/Scene/rooms/stels-house").AddChild(bagInstance);
        bagInstance.ChestHandler.ItemCodes.Add(itemInBag);
        Node3D bagSpawn = GetNode<Node3D>(bagSpawnPath);
        bagInstance.GlobalTransform = Global.SetNewOrigin(bagInstance.GlobalTransform, bagSpawn.GlobalTransform.Origin);
        bag = bagInstance;
    }

    public void OnFoundEnemy()
    {
        if (!connected) return;
        
        foreach (var eye in eyes)
        {
            eye?.MakeActive(false);
        }
        
        audi.Stream = beepSound;
        audi.Play();
        assistantPie.dialogueCode = loseDialogue;
        bag.QueueFree();
        player.TakeItem -= OnPlayerTakeItem;
        connected = false;
        
        checkButton = true;
    }

    public void OnPlayerTakeItem(string itemCode)
    {
        if (itemCode != itemInBag) return;
        if (trainingIsDone) return;
        
        foreach (var eye in eyes)
        {
            eye?.MakeActive(false);
        }
        audi.Stream = beepSound;
        audi.Play();
        assistantPie.dialogueCode = winDialogue;
        
        player.TakeItem -= OnPlayerTakeItem;
        connected = false;
        
        trainingIsDone = true;
        SetActive(false);
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
        if (data["eyesPath"].Obj is Array eyesPath)
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
        if (data.TryGetValue("bagPath", out var bagPath))
        {
            bag = GetNode<FurnChest>(bagPath.ToString());
        }
        
        //загружаем событие
        connected = (bool) data["eventConnected"];
        if (connected)
        {
            player.TakeItem += OnPlayerTakeItem;
        }
    }
}
