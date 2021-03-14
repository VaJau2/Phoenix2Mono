using Godot;
using Godot.Collections;
using System.Globalization;

//Синглтон-класс для управления паузами и для всяких универсальных методов
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

    public bool paused;
    public bool mainMenuFirstTime = true;

    public Player player;
    public Race playerRace = Race.Earthpony;

    public Settings Settings;

    public void SetPause(Node self, bool pause, bool pauseMusicArg = true) 
    {
        bool pauseMusic = pauseMusicArg;
        if (pauseMusicArg) {
            pauseMusic = pause;
        }
        SetPauseMusic(pauseMusic);

        self.GetTree().Paused = pause;
        paused = pause;
        if (pause) {
            Input.SetMouseMode(Input.MouseMode.Visible);
        } else {
            Input.SetMouseMode(Input.MouseMode.Captured);
        }
    }

    public void SetPauseMusic(bool pause)
    {
        if (player == null) {
            return;
        }

        foreach(var tempObj in player.GetTree().GetNodesInGroup("unpaused_sound")) {
            if (tempObj is AudioStreamPlayer) {
                var tempAudi = tempObj as AudioStreamPlayer;
                tempAudi.StreamPaused = pause;
            }
        }
    }

    public void LoadSettings(Node menu) 
    {
        Settings = new Settings(menu);
        Settings.LoadSettings();
    }

    public static Race RaceFromString(string raceString) 
    {
        switch(raceString) {
            case "earthpony": return Race.Earthpony;
            case "pegasus": return Race.Pegasus;
            case "unicorn": return Race.Unicorn;
        }
        return Race.Earthpony;
    }

    public static Dictionary loadJsonFile(string filePath)
    {
        File tempFile = new File();
        string path = "res://" + filePath;

        Error fileError = tempFile.Open(path, File.ModeFlags.Read);
        if (fileError == Error.Ok) {
            var text_json = tempFile.GetAsText();
            tempFile.Close();
            var result_json = JSON.Parse(text_json);

            if (result_json.Error == Error.Ok) {  
                return (Dictionary)result_json.Result;
            }
        }
        GD.PrintErr("error loading JSON file in: " + path);
        return null;
    }

    public static Transform setNewOrigin(Transform transform, Vector3 newOrigin)
    {
        Transform tempTrans = transform;
        tempTrans.origin = newOrigin;
        return tempTrans;
    }

    public SignalAwaiter ToTimer(float time, Node _object = null) 
    {
        if (_object == null)
        {
            _object = player;
        }
        return _object.ToSignal(_object.GetTree().CreateTimer(time), "timeout");
    }

    //nominativ - 1 бит
    //genetiv   - 2 бита
    //plural    - 5 битов
    public static string GetCountWord(int number, string nominativ, string genetiv, string plural) 
    {
        var titles = new[] {nominativ, genetiv, plural};
        var cases = new[] {2, 0, 1, 1, 1, 2};
        return titles[number % 100 > 4 && number % 100 < 20 ? 2 : cases[(number % 10 < 5) ? number % 10 : 5]];
    }

    public static string GetKeyName(string actionName) 
    {
        var actions = InputMap.GetActionList(actionName);
        var action = actions[0] as InputEventKey;
        return OS.GetScancodeString(action.Scancode);
    }

    public static float ParseFloat(string value) => float.Parse(value, CultureInfo.InvariantCulture);
}

public enum Race {
    Earthpony,
    Unicorn,
    Pegasus
}