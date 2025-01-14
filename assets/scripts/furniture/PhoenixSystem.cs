using System;
using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

public class PhoenixSystem : Node, ISavable
{
    [Export] private NodePath onDeathTriggerPath;
    [Export] private NodePath roomPath;
    [Export] private List<NodePath> cloneFlasksPaths;
    
    public bool CloneWokeUp;
    
    private Room room;
    private RoomManager roomManager;
    
    private int cloneNumber;
    private readonly List<CloneFlask> cloneFlasks = [];
    private CloneFlaskTrigger cloneFlaskTrigger;
    private PlayerDeathManager deathManager;
    private PlayerSpawner playerSpawner;
    
    private LevelsLoader levelsLoader;
    
    [Signal]
    public delegate void CloneAwake();

    public override void _Ready()
    {
        room = GetNode<Room>(roomPath);
        roomManager = GetNode<RoomManager>("/root/Main/Scene/rooms");
        cloneFlaskTrigger = GetNode<CloneFlaskTrigger>("Clone Flask Trigger");
        playerSpawner = GetNode<PlayerSpawner>("/root/Main/Scene/PlayerSpawner");
        levelsLoader = GetNode<LevelsLoader>("/root/Main");
        
        foreach (var cloneFlask in cloneFlasksPaths.Select(GetNodeOrNull<CloneFlask>))
        {
            cloneFlasks.Add(cloneFlask);
        }

        cloneNumber = cloneFlasks.Count - 1;
        
        levelsLoader.Connect(nameof(LevelsLoader.SaveDataLoaded), this, nameof(OnSaveDataLoaded));
    }

    private async void OnSaveDataLoaded()
    {
        await ToSignal(GetTree(), "idle_frame");

        if (CloneWokeUp)
        {
            playerSpawner.InitSpawn();
            
            await ToSignal(GetTree(), "idle_frame");
            
            var player = Global.Get().player;
            levelsLoader.LoadObjectData(player.Name);
            
            deathManager = player.DeathManager;
            deathManager.PermanentDeath = cloneFlasks.Count < 1;
            InitDeathTrigger();
            
            if (deathManager.PermanentDeath) return;
            
            deathManager.Connect(nameof(PlayerDeathManager.CloneDie), this, nameof(OnCloneDie));
        }
        else StartCloning();
    }

    private void InitDeathTrigger()
    {
        if (onDeathTriggerPath == null) return;
        
        var deathTrigger = GetNodeOrNull<TriggerBase>(onDeathTriggerPath);

        if (deathTrigger == null) return;
        
        deathManager.OnDeathTrigger = deathTrigger;
    }
    
    private void OnCloneDie()
    {
        var player = deathManager.GetParent<Player>();
        DeletePlayer(player);
        CreateDeadBody(player);
        StartCloning();
    }

    private static void DeletePlayer(Player player)
    {
        Global.Get().player = null;
        player.Body.DetachFromPlayer();
        
        foreach (Node node in player.GetChildren())
        {
            if (node.Name == "player_body") continue;
            node.SetProcess(false);
            node.QueueFree();
        }
    }

    private void CreateDeadBody(Player player)
    {
        Array<string> playerItems = [];

        var weapon = player.Inventory.weapon;
        if (!string.IsNullOrEmpty(weapon))
        {
            playerItems.Add(weapon);
        }
        
        var cloth = player.Inventory.cloth;
        if (cloth != "empty")
        {
            playerItems.Add(cloth);
        }

        var artifact = player.Inventory.artifact;
        if (!string.IsNullOrEmpty(artifact))
        {
            playerItems.Add(artifact);
        }
        
        foreach (var itemButton in player.Inventory.menu.mode.itemButtons)
        {
            playerItems.Add(itemButton.myItemCode);
        }
        
        var playerPath = player.GetPath();
        var playerDeadScript = ResourceLoader.Load("res://assets/scripts/characters/player/PlayerDead.cs");
        player.SetScript(playerDeadScript);
        
        var playerDead = GetNode<PlayerDead>(playerPath);
        playerDead.Name = $"Created_Player_{cloneNumber}";
        playerDead._Ready();
        playerDead.Set(
            Global.Get().playerRace, 
            player.Inventory.cloth,
            player.Inventory.artifact,
            false
        );
        
        foreach (var playerItem in playerItems)
        {
            playerDead.ChestHandler.ItemCodes.Add(playerItem);
        }
    }

    private async void StartCloning()
    {
        if (cloneFlasks.Count == 0)
        {
            deathManager = null;
            return;
        }
        
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
        deathManager.PermanentDeath = cloneFlasks.Count < 1;
        InitDeathTrigger();
        
        deathManager.Connect(nameof(PlayerDeathManager.CloneDie), this, nameof(OnCloneDie));
        
        EmitSignal(nameof(CloneAwake));
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            { "cloneNumber", cloneNumber },
            { "cloneWokeUp", CloneWokeUp },
            { "playerRace", Global.RaceToString(Global.Get().playerRace) }
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
        Global.Get().playerRace = Global.RaceFromString(data["playerRace"].ToString());
    }
}