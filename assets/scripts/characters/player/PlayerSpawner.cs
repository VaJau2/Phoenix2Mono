using Godot;
using Godot.Collections;

public class PlayerSpawner : Spatial
{
    [Export]
    public int moneyCount = 0;
    [Export]
    public Array<string> itemCodes = new Array<string>();
    [Export]
    public Dictionary<string, int> ammo = new Dictionary<string, int>();

    [Export]
    public string clothCode = "";

    [Export] public bool checkSavedData = true;
    [Export] public bool spawningInside = false;

    public bool loadStartItems = true;
   

    public override void _Ready()
    {
        Global global = Global.Get();
        string path = "res://objects/characters/Player/Player_";

        switch(global.playerRace) {
            case (Race.Earthpony):
                path += "Earthpony.tscn";
                break;
            case (Race.Pegasus):
                path += "Pegasus.tscn";
                break;
            case (Race.Unicorn):
                path += "Unicorn.tscn";
                break;
        }

        var playerPrefab = GD.Load<PackedScene>(path);
        var newPlayer = (Player)playerPrefab.Instance();

        SpawnPlayer(newPlayer);
    }

    private async void SpawnPlayer(Player player) {
        await ToSignal(GetTree(), "idle_frame");
        GetParent().AddChild(player);
        player.GlobalTransform = GlobalTransform;
        player.Camera.Current = true;

        if (loadStartItems)
        {
            //загрузка инвентаря во время перехода между уровнями
            if (checkSavedData)
            {
                var savedData = GetNodeOrNull<SaveNode>("/root/Main/SavedData");
                if (savedData != null)
                {
                    player.inventory.LoadData(savedData.InventoryData);
                    savedData.QueueFree();
                    QueueFree();
                    return;
                }
            }

            //загрузка стартовых вещей
            player.inventory.money = moneyCount;
            player.inventory.LoadItems(itemCodes, ammo);

            //загрузка надетой на ГГ брони
            if (clothCode != "" && clothCode != "empty")
            {
                player.inventory.LoadWearItem(clothCode, "armor");
            }

            //если единорог спавнится в помещении
            if (player is Player_Unicorn unicorn && spawningInside)
            {
                unicorn.teleportInside = true;
            }
        }

        QueueFree();
    }
}
