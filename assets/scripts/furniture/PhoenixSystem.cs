using System;
using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

public class PhoenixSystem : Node, ISavable
{
    public bool CloneWokeUp;
    
    [Export] private NodePath roomPath;
    private Room room;
    
    [Export] private List<NodePath> cloneFlasksPaths;
    private readonly List<CloneFlask> cloneFlasks = [];
    private int cloneNumber;
    
    private CloneFlaskTrigger cloneFlaskTrigger;
    private PlayerDeathManager deathManager;
    private RoomManager roomManager;
    
    [Signal]
    public delegate void CloneAwake();

    public override void _Ready()
    {
        roomManager = GetNode<RoomManager>("/root/Main/Scene/rooms");
        room = GetNode<Room>(roomPath);
        
        cloneFlaskTrigger = GetNode<CloneFlaskTrigger>("Clone Flask Trigger");
        
        foreach (var cloneFlask in cloneFlasksPaths.Select(GetNodeOrNull<CloneFlask>))
        {
            cloneFlasks.Add(cloneFlask);
        }

        cloneNumber = cloneFlasks.Count - 1;

        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");

        if (CloneWokeUp)
        {
            deathManager = Global.Get().player.DeathManager;
            deathManager.permanentDeath = cloneFlasks.Count < 1;
            
            if (deathManager.permanentDeath) return;
            
            deathManager.Connect(nameof(PlayerDeathManager.CloneDie), this, nameof(OnCloneDie));
        }
        else StartCloning();
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
        oldPlayer.SetScript(null);
        
        StartCloning();
    }

    private async void StartCloning()
    {
        if (cloneFlasks.Count == 0) return;
        
        var cloneFlask = cloneFlasks[cloneNumber];
        Global.Get().playerRace = cloneFlask.GetRace();
        cloneFlaskTrigger.Resurrect(cloneFlask, this);
        cloneFlasks.Remove(cloneFlask);
        cloneNumber = cloneFlasks.Count - 1;
        
        await ToSignal(GetTree(), "idle_frame");

        var player = Global.Get().player;
        
        if (roomManager.CurrentRoom != room)
        {
            if (roomManager.CurrentRoom != null)
            {
                roomManager.CurrentRoom.Visible = false;
                roomManager.CurrentRoom.Exit();
            }
            
            room.Visible = true;
            room.Enter();
        }
        else
        {
            player.AudioEffectsController.AddEffects(room.Name);
        }
        
        deathManager = player.DeathManager;
        deathManager.permanentDeath = cloneFlasks.Count < 1;
        deathManager.Connect(nameof(PlayerDeathManager.CloneDie), this, nameof(OnCloneDie));
        
        EmitSignal(nameof(CloneAwake));
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
        if (!data.Contains("cloneNumber")) return;
        
        cloneNumber = Convert.ToInt32(data["cloneNumber"]);
        
        for (int i = cloneFlasks.Count - 1; i > cloneNumber; i--)
        {
            cloneFlasks.RemoveAt(i);
        }

        CloneWokeUp = Convert.ToBoolean(data["cloneWokeUp"]);
    }
}