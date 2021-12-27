using System;
using Godot;
using Godot.Collections;
using Array = Godot.Collections.Array;

public class SpawnerNpcTrigger: ActivateOtherTrigger
{
    [Export] private PackedScene npcPrefab;
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

    private Spatial spawnPoint;
    private Spatial movePoint;
    
    private SavableTimers timers;
    private int step;

    public override void _Ready()
    {
        if (spawnPointPath != null)
        {
            spawnPoint = GetNode<Spatial>(spawnPointPath);
        }
        if (movePointPath != null)
        {
            movePoint = GetNode<Spatial>(movePointPath);
        }

        timers = GetNode<SavableTimers>("/root/Main/Scene/timers");
        base._Ready();
    }

    public override void SetActive(bool newActive)
    {
        _on_activate_trigger();
        base.SetActive(newActive);
    }

    public override void _on_activate_trigger()
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

        base._on_activate_trigger();
    }

    private async void WaitStartDelay()
    {
        step = 1;
        while (timers.CheckTimer(Name + "_timer", spawnDelay))
        {
            await ToSignal(GetTree(), "idle_frame");
        }

        step = 2;
        _on_activate_trigger();
    }

    private void SpawnNpc()
    {
        if (npcPrefab.Instance() is NpcWithWeapons npcInstance)
        {
            npcInstance.Name = "Created_" + npcInstance.Name;
            npcInstance.StartHealth = StartHealth;
            npcInstance.weaponCode = weaponCode;
            npcInstance.relation = relation;
            npcInstance.itemCodes = itemCodes;
            npcInstance.ammoCount = ammoCount;
            GetNode<Node>("/root/Main/Scene/npc").AddChild(npcInstance);
            npcInstance.GlobalTransform = Global.setNewOrigin(npcInstance.GlobalTransform, spawnPoint.GlobalTransform.origin);
            npcInstance.Rotation = new Vector3(0, spawnPoint.Rotation.y,0);
            if (movePoint != null)
            {
                npcInstance.SetNewStartPos(movePoint.GlobalTransform.origin, run);
                npcInstance.myStartRot = new Vector3(0, movePoint.Rotation.y, 0);
            }
            else
            {
                npcInstance.SetNewStartPos(spawnPoint.GlobalTransform.origin, run);
                npcInstance.myStartRot = new Vector3(0, spawnPoint.Rotation.y, 0);
            }
            
            if (triggerConnections != null)
            {
                foreach (var triggerDataPrimary in triggerConnections)
                {
                    if (!(triggerDataPrimary is Array triggerData) || triggerData.Count != 4) continue;

                    var trigger = GetNode(triggerData[0].ToString());
                    var signal = triggerData[1].ToString();
                    var method = triggerData[2].ToString();
                    var binds = triggerData[3] as Array;
                    npcInstance.Connect(signal, trigger, method, binds);
                }
            }
        }
    }
    
    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["step"] = step;
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        step = Convert.ToInt16(data["step"]);
        if (step > 0)
        {
            _on_activate_trigger();
        }
    }
}
