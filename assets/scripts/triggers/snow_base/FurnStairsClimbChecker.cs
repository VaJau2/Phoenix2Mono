using Godot;

namespace FurnStairs;

public enum MayClimbOption
{
    NeedTable,
    NeedSofa,
    NeedChair,
    NeedKey,
    MayClimb
}

public class FurnStairsClimbChecker
{
    private readonly FurnDoor ventDoor;
    private readonly Spatial furnsParent;

    public FurnStairsClimbChecker(FurnDoor ventDoor, Spatial furnsParent)
    {
        this.ventDoor = ventDoor;
        this.furnsParent = furnsParent;
    }
    
    public MayClimbOption CheckMayClimb(Player player)
    {
        if (furnsParent.GetNode("table").GetChildCount() == 0)
        {
            return MayClimbOption.NeedTable;
        }

        if (furnsParent.GetNode("sofa").GetChildCount() == 0)
        {
            return MayClimbOption.NeedSofa;
        }
        
        if (furnsParent.GetNode("chair").GetChildCount() == 0)
        {
            return MayClimbOption.NeedChair;
        }

        if (ventDoor is { IsOpen: false, myKey: not null } && !player.Inventory.HasItem(ventDoor.myKey))
        {
            return MayClimbOption.NeedKey;
        }

        return MayClimbOption.MayClimb;
    }
}
