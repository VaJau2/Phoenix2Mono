using Godot;
using Godot.Collections;

public class PlayerSpawner : Spatial
{
    [Export]
    public bool HaveCoat;
    [Export]
    public Array<WeaponTypes> StartWeapons = new Array<WeaponTypes>();

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
        newPlayer.HaveCoat = HaveCoat;
        newPlayer.StartWeapons = StartWeapons;
        
        SpawnPlayer(newPlayer);
    }

    private async void SpawnPlayer(Player player) {
        await ToSignal(GetTree(), "idle_frame");
        GetParent().AddChild(player);
        player.GlobalTransform = GlobalTransform;
        player.Camera.Current = true;

        QueueFree();
    }

}
