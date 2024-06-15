using Godot;

public class BagSpawner
{
    private const float RAY_FIRST_DISTANCE = 5f;
    private const float RAY_THIRD_DISTANCE = 10f;
    private PackedScene bagPrefab;
    
    private Player player => Global.Get().player;

    public BagSpawner()
    {
        bagPrefab = GD.Load<PackedScene>("res://objects/props/furniture/bag.tscn");
    }

    public IChest SpawnItemBag()
    {
        var newBag = (BagChest)bagPrefab.Instance();
        Node parent = player.GetNode("/root/Main/Scene");
        parent.AddChild(newBag);
        newBag.Name = "Created_" + newBag.Name;
        newBag.GlobalTranslation = GetSpawnPoint();
        return newBag;
    }
    
    private Vector3 GetSpawnPoint()
    {
        var distance = player.ThirdView ? RAY_THIRD_DISTANCE : RAY_FIRST_DISTANCE;
        
        var tempRay = player.Camera.UseRay(distance);
        tempRay.ForceRaycastUpdate();

        var point = tempRay.IsColliding()
            ? tempRay.GetCollisionPoint()
            : player.GlobalTranslation;
        
        player.Camera.ReturnRayBack();

        return point;
    }
}