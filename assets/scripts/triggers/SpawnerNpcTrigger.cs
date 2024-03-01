using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public partial class SpawnerNpcTrigger: ActivateOtherTrigger
{
    [Export] private PackedScene npcPrefab;
    [Export] private string npcName;
    [Export] private NodePath spawnPointPath;
    [Export] private float spawnDelay;
    [Export] private NodePath movePointPath;
    [Export] private bool run;

    [Export] private int StartHealth = 100;
    [Export] private string weaponCode;
    [Export] private Relation relation;
    [Export]
    private Array<string> itemCodes = new Array<string>();
    [Export]
    private Dictionary<string, int> ammoCount = new Dictionary<string, int>();
    
    //0 - путь до триггера, строка
    //1 - сигнал нпц, строка
    //2 - метод триггера, строка
    //3 - binds, массив
    [Export] private Array triggerConnections;

    private Node3D spawnPoint;
    private Node3D movePoint;

    private double tempTimer;
    private int step;

    public override void _Ready()
    {
        SetProcess(false);
        if (spawnPointPath != null)
        {
            spawnPoint = GetNode<Node3D>(spawnPointPath);
        }
        if (movePointPath != null)
        {
            movePoint = GetNode<Node3D>(movePointPath);
        }

        base._Ready();
    }
    
    public override void _Process(double delta)
    {
        if (tempTimer > 0)
        {
            tempTimer -= delta;
            return;
        }

        SetProcess(false);
        step = 2;
        OnActivateTrigger();
    }

    public override void SetActive(bool newActive)
    {
        OnActivateTrigger();
        base.SetActive(newActive);
    }

    public override void OnActivateTrigger()
    {
        if (!IsActive) return;

        switch (step)
        {
            case 0:
            case 1:
                WaitStartDelay();
                return;
            case 2:
                SpawnNpc();
                break;
        }

        base.OnActivateTrigger();
    }

    private void WaitStartDelay()
    {
        step = 1;
        tempTimer = spawnDelay;
        SetProcess(true);
    }

    private void SpawnNpc()
    {
        if (npcPrefab.Instantiate<NpcWithWeapons>() is not { } npcInstance) 
            return;
        
        npcInstance.Name = "Created_" + (!string.IsNullOrEmpty(npcName) ? npcName : npcInstance.Name);
        npcInstance.StartHealth = StartHealth;
        npcInstance.weaponCode = weaponCode;
        npcInstance.relation = relation;
        npcInstance.itemCodes = itemCodes;
        npcInstance.ammoCount = ammoCount;
        GetNode<Node>("/root/Main/Scene/npc").AddChild(npcInstance);
        npcInstance.GlobalTransform = Global.SetNewOrigin(npcInstance.GlobalTransform, spawnPoint.GlobalTransform.Origin);
        npcInstance.Rotation = new Vector3(0, spawnPoint.Rotation.Y,0);
        if (movePoint != null)
        {
            npcInstance.SetNewStartPos(movePoint.GlobalTransform.Origin, run);
            npcInstance.myStartRot = new Vector3(0, movePoint.Rotation.Y, 0);
        }
        else
        {
            npcInstance.SetNewStartPos(spawnPoint.GlobalTransform.Origin, run);
            npcInstance.myStartRot = spawnPoint.Rotation;
        }
            
        if (triggerConnections != null)
        {
            foreach (var triggerDataPrimary in triggerConnections)
            {
                if (triggerDataPrimary.Obj is not Array { Count: 4 } triggerData) continue;

                var trigger = GetNode(triggerData[0].ToString());
                var signal = triggerData[1].ToString();
                var method = triggerData[2].ToString();
                npcInstance.Connect(signal, new Callable(trigger, method));
            }
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
        
        if (data.TryGetValue("tempTimer", out var timerValue))
        {
            tempTimer = timerValue.AsSingle();
        }

        step = data["step"].AsInt16();
        if (step > 0)
        {
            OnActivateTrigger();
        }
    }
}
