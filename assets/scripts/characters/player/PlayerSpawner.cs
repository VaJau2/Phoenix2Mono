using System;
using Godot;
using Godot.Collections;

public class PlayerSpawner : Spatial
{
    [Export]
    public int moneyCount;
    [Export]
    public Array<string> itemCodes = new();
    [Export]
    public Dictionary<string, int> ammo = new();

    [Export]
    public string clothCode = "";

    [Export] public bool checkSavedData = true;
    [Export] public bool checkSavedMoney = true;
    [Export] public bool spawningInside;
    [Export] private bool deleteAfterSpawn = true;

    public bool loadStartItems = true;

    [Signal]
    public delegate void Spawned();

    public override void _Ready()
    {
        InitSpawn();
    }

    public void InitSpawn()
    {
        var path = "res://objects/characters/Player/Player_";
        path += GetRacePath();

        var playerPrefab = GD.Load<PackedScene>(path);
        var newPlayer = (Player)playerPrefab.Instance();

        SpawnPlayer(newPlayer);
    }

    private static string GetRacePath()
    {
        return Global.Get().playerRace switch
        {
            Race.Earthpony => "Earthpony.tscn",
            Race.Pegasus => "Pegasus.tscn",
            Race.Unicorn => "Unicorn.tscn",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private async void SpawnPlayer(Player player) 
    {
        await ToSignal(GetTree(), "idle_frame");
        GetParent().AddChild(player);
        player.GlobalTransform = GlobalTransform;
        player.Camera.Current = true;

        //если единорог спавнится в помещении
        if (player is Player_Unicorn unicorn && spawningInside)
        {
            unicorn.teleportInside = true;
        }
        
        if (loadStartItems)
        {
            //загрузка сохраненного инвентаря во время перехода между уровнями
            if (checkSavedData || checkSavedMoney)
            {
                var savedData = GetNodeOrNull<SaveNode>("/root/Main/SaveNode");
                if (IsInstanceValid(savedData))
                {
                    if (checkSavedMoney && savedData.InventoryData.Contains("money"))
                    {
                        var loadedMoney = savedData.InventoryData["money"].ToString();
                        player.Inventory.LoadMoney(loadedMoney);
                    }
                    if (checkSavedData)
                    {
                        player.Inventory.LoadData(savedData.InventoryData);
                        savedData.CheckClonedSaveData();
                        QueueFree();
                        return;
                    }
                }
            }
            
            //если деньги не переносятся между уровнями, загружается стартовое значение
            if (!checkSavedMoney)
            {
                player.Inventory.money = moneyCount;
            }
            
            //загрузка стартовых вещей
            player.Inventory.LoadItems(itemCodes, ammo);

            //загрузка надетой на ГГ брони
            if (clothCode != "" && clothCode != "empty")
            {
                player.Inventory.LoadWearItem(clothCode, ItemType.armor);
            }
        }

        if (deleteAfterSpawn)
        {
            QueueFree();
            return;
        }
        
        EmitSignal(nameof(Spawned));
    }
}
