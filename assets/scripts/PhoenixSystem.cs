using System;
using Godot;
using Godot.Collections;
using System.Collections.Generic;

public class PhoenixSystem : Node, ISavable
{
    public bool CloneWokeUp;
    
    [Export] private NodePath roomPath;
    private Room room;
    
    [Export] private List<NodePath> cloneFlasksPathes;
    private List<CloneFlask> cloneFlasks = new ();
    private int cloneNumber;
    
    private CloneFlaskTrigger cloneFlaskTrigger;
    private PlayerDeathManager deathManager;
    private RoomManager roomManager;

    public override void _Ready()
    {
        roomManager = GetNode<RoomManager>("/root/Main/Scene/rooms");
        room = GetNode<Room>(roomPath);
        
        cloneFlaskTrigger = GetNode<CloneFlaskTrigger>("Clone Flask Trigger");
        
        foreach (var path in cloneFlasksPathes)
        {
            var cloneFlask = GetNodeOrNull<CloneFlask>(path);
            cloneFlasks.Add(cloneFlask);
        }

        cloneNumber = cloneFlasks.Count - 1;

        var levelsLoader = GetNode<LevelsLoader>("/root/Main");
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");
        
        deathManager = Global.Get().player.DeathManager;
        if (cloneNumber > 0) deathManager.permanentDeath = false;
        deathManager.Connect(nameof(PlayerDeathManager.CloneDie), this, nameof(OnCloneDie));
        
        if (cloneFlasks.Count == 0) return;

        if (cloneFlasks.Count == 1)
        {
            deathManager.permanentDeath = true;
        }
        
        if (CloneWokeUp) return;
        
        var cloneFlask = cloneFlasks[cloneNumber];
        cloneFlaskTrigger.Resurrect(cloneFlask, this);
        cloneFlasks.Remove(cloneFlask);
        cloneNumber = cloneFlasks.Count - 1;
    }
    
    private void OnCloneDie()
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
        cloneFlaskTrigger.Resurrect(cloneFlask, this);
        cloneFlasks.Remove(cloneFlask);
        cloneNumber = cloneFlasks.Count - 1;
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