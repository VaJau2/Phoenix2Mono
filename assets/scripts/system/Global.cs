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
    public Race playerRace = Race.Pegasus;

    public Settings Settings = new Settings();


    public static Transform setNewOrigin(Transform transform, Vector3 newOrigin)
    {
        Transform tempTrans = transform;
        tempTrans.origin = newOrigin;
        return tempTrans;
    }

    public SignalAwaiter ToTimer(float time) {
        return player.ToSignal(player.GetTree().CreateTimer(time), "timeout");
    }
}

public enum Race {
    Earthpony,
    Unicorn,
    Pegasus
}