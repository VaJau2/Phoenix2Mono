using Godot;
public class Global {
    //----Using singleton pattern----
    private static Global instance;

    private Global() {}

    public static Global Get() 
    {
        if (instance == null) {
            instance = new Global();
        }
        return instance;
    }
    //-----------------------------

    public Player player {set; private get;}
    public Player GetPlayer() 
    {
        if (player != null) {
            return player;
        }
        GD.PrintErr("someone tried to get player, but player is null :c");
        return null;
    }

    public Race playerRace = Race.Unicorn;


    public static Transform setNewOrigin(Transform transform, Vector3 newOrigin)
    {
        Transform tempTrans = transform;
        tempTrans.origin = newOrigin;
        return tempTrans;
    }
}

public enum Race {
    Earthpony,
    Unicorn,
    Pegasus
}