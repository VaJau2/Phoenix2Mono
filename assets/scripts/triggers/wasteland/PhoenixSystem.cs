using System;
using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

public partial class PhoenixSystem : Node, ISavable
{
    public bool CloneWokeUp;
    
    [Export] private NodePath roomPath;
    private Room room;
    
    [Export] private Array<NodePath> cloneFlasksPathes;
    private List<CloneFlask> cloneFlasks = new ();
    private int cloneNumber;
    
    private CloneFlaskTrigger cloneFlaskTrigger;
    private PlayerSpawner playerSpawner;
    private PlayerDeathManager deathManager;
    private RoomManager roomManager;

    public override void _Ready()
    {
        roomManager = GetNode<RoomManager>("/root/Main/Scene/rooms");
        room = GetNode<Room>(roomPath);
        
        cloneFlaskTrigger = GetNode<CloneFlaskTrigger>("Clone Flask Trigger");
        
        foreach (var cloneFlask in cloneFlasksPathes.Select(GetNodeOrNull<CloneFlask>))
        {
            cloneFlasks.Add(cloneFlask);
        }

        cloneNumber = cloneFlasks.Count - 1;

        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.SaveDataLoaded += OnSaveDataLoaded;
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "process_frame");
        
        if (CloneWokeUp) return;
        StartCloning();
    }
    
    private void OnCloneDie()
    {
        var oldPlayer = Global.Get().player;
        Global.Get().player = null;
        
        foreach (Node node in oldPlayer.GetChildren())
        {
            if (node.Name == "player_body") continue;
            node.SetProcess(false);
            node.QueueFree();
        }

        oldPlayer.Body.DetachFromPlayer();
        oldPlayer.SetScript(new Variant());
        
        StartCloning();
    }

    private async void StartCloning()
    {
        if (cloneFlasks.Count == 0) return;

        if (cloneFlasks.Count == 1)
        {
            deathManager.permanentDeath = true;
        }

        if (room != roomManager.CurrentRoom)
        {
            if (roomManager.CurrentRoom != null)
            {
                roomManager.CurrentRoom.Visible = false;
                roomManager.CurrentRoom.Exit();
            }
            
            room.Visible = true;
            room.Enter();
        }
        
        var cloneFlask = cloneFlasks[cloneNumber];
        Global.Get().playerRace = cloneFlask.GetRace();
        cloneFlaskTrigger.Resurrect(cloneFlask, this);
        cloneFlasks.Remove(cloneFlask);
        cloneNumber = cloneFlasks.Count - 1;

        await ToSignal(GetTree(), "process_frame");
        
        deathManager = Global.Get().player.DeathManager;
        if (cloneNumber > 0) deathManager.permanentDeath = false;
        deathManager.CloneDie += OnCloneDie;
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "cloneNumber", cloneNumber },
            { "cloneWokeUp", CloneWokeUp }
        };
    }

    public void LoadData(Dictionary data)
    {
        if (!data.ContainsKey("cloneNumber")) return;
        
        cloneNumber = Convert.ToInt32(data["cloneNumber"]);
        
        for (int i = cloneFlasks.Count - 1; i > cloneNumber; i--)
        {
            cloneFlasks.RemoveAt(i);
        }

        CloneWokeUp = Convert.ToBoolean(data["cloneWokeUp"]);
    }
}