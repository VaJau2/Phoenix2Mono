using Godot;

public class PlayerWeapons 
{
    Player player;
    RayCast rayFirst;
    RayCast rayThird;

    public PlayerWeapons(Player player) 
    {
        this.player = player;

        rayFirst = player.GetNode<RayCast>("rotation_helper/camera/ray");
        rayThird = player.GetNode<RayCast>("rotation_helper_third/camera/ray");
        rayFirst.AddException(player);
        rayThird.AddException(player);
    }

    public RayCast EnableHeadRay(float distance)
    {
        var tempRay = rayFirst;
        if(player.ThirdView) 
        {
            tempRay = rayThird;
        }
        tempRay.CastTo = new Vector3(0,0,-distance);
        tempRay.Enabled = true;
        return tempRay;
    }
}