using Godot;
using Godot.Collections;

public class PlayerSpawner : Spatial
{
    [Export]
    public Array<string> itemCodes = new Array<string>();
    [Export]
    public Dictionary<string, int> ammo = new Dictionary<string, int>();

    [Export]
    public string clothCode;
   

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
        player.inventory.LoadItems(itemCodes, ammo);

        QueueFree();
    }

}
