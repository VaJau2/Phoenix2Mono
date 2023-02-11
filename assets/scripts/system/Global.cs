using System.Collections;
using Godot;
using Godot.Collections;
using System.Globalization;
using System.Collections.Generic;

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
    
    //массив файлов сохранений
    public static List<FileTableLine> saveFilesArray = new List<FileTableLine>();
    public static Array<string> deletedObjects { get; private set; } = new Array<string>();

    private static Dictionary jsonCache;
    private static string jsonCachePath;

    public static void AddDeletedObject(string name)
    {
        if (!name.BeginsWith("Created_") && !deletedObjects.Contains(name))
        {
            deletedObjects.Add(name);
        } 
    }
    
    //Поиск нода в сцене по его имени
    public static Node FindNodeInScene(Node scene, string name)
    {
        var queue = new Queue<Node>();
        queue.Enqueue(scene);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node == null) continue;

            if (node.Name == name)
                return node;

            foreach (Node child in node.GetChildren())
            {
                queue.Enqueue(child);
            }
        }

        return null;
    }

    public void SetPause(Node self, bool pause, bool pauseMusicArg = true) 
    {
        bool pauseMusic = pauseMusicArg;
        if (pauseMusicArg) {
            pauseMusic = pause;
        }
        SetPauseMusic(pauseMusic);

        self.GetTree().Paused = pause;
        paused = pause;
        Input.MouseMode = pause ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
    }

    public void SetPauseMusic(bool pause)
    {
        if (player == null) 
        {
            return;
        }

        foreach(var tempObj in player.GetTree().GetNodesInGroup("unpaused_sound")) 
        {
            if (tempObj is AudioStreamPlayer tempAudi) 
            {
                tempAudi.StreamPaused = pause;
            }
        }
    }

    public void LoadSettings(Node menu) 
    {
        if (Settings == null)
        {
            foreach (string saveFileName in GetSaveFiles())
            {
                var fileData = GetFileMetadata(saveFileName);
                if (fileData is FileTableLine line)
                {
                    saveFilesArray.Add(line);
                }
            }
            
            Settings = new Settings(menu);
        }
        
        Settings.LoadSettings();
    }

    public static Race RaceFromString(string raceString) 
    {
        switch(raceString) 
        {
            case "earthpony": return Race.Earthpony;
            case "pegasus": return Race.Pegasus;
            case "unicorn": return Race.Unicorn;
        }
        return Race.Earthpony;
    }

    public static string RaceToString(Race race)
    {
        switch(race) 
        {
            case Race.Earthpony: return "earthpony";
            case Race.Pegasus:   return "pegasus";
            case Race.Unicorn:   return "unicorn";
        }
        return "earthpony";
    }

    public static Dictionary loadJsonFile(string filePath)
    {
        if (jsonCachePath == filePath)
        {
            return jsonCache;
        }
        
        File tempFile = new File();
        string path = "res://" + filePath;

        Error fileError = tempFile.Open(path, File.ModeFlags.Read);
        if (fileError == Error.Ok) 
        {
            var textJson = tempFile.GetAsText();
            tempFile.Close();
            var resultJson = JSON.Parse(textJson);

            if (resultJson.Error == Error.Ok) 
            {  
                jsonCachePath = filePath;
                jsonCache = (Dictionary)resultJson.Result;
                return jsonCache;
            }
            
            GD.PrintErr("parse json (" + filePath  + ") error: " + resultJson.ErrorString + ", in line: " + resultJson.ErrorLine);
            return null;
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

    public SignalAwaiter ToTimer(float time, Node _object = null, bool pauseProcess = false)
    {
        var newTimer = new Timer {Autostart = true, OneShot = true, WaitTime = time};
        if (pauseProcess)
        {
            newTimer.PauseMode = Node.PauseModeEnum.Process;
        }

        if (Object.IsInstanceValid(_object))
        {
            _object?.AddChild(newTimer);
        }
        else
        {
            player.GetNode("/root/Main/Scene").AddChild(newTimer);
        }

        newTimer.Connect("timeout", newTimer, "queue_free");
        return newTimer.ToSignal(newTimer, "timeout");
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

    //возвращает название кнопки клавиатуры по её экшну
    public static string GetKeyName(string actionName) 
    {
        var actions = InputMap.GetActionList(actionName);
        if (actions.Count == 0) return null;
        if (!(actions[0] is InputEventKey action)) return null;
        return OS.GetScancodeString(action.Scancode);
    }

    public static float ParseFloat(string value) => float.Parse(value, CultureInfo.InvariantCulture);


    //уменьшает длину строки, разбивая длинные строки на подстроки
    //разбивает по точкам, запятым и пробелам
    public static Array ClumpLineLength(Array lines, int maxLineLength)
    {
        Array result = new Array();
        foreach(string line in lines)
        {
            if (line.Length <= maxLineLength) {
                result.Add(line);
            } else {
                Array<char> charsForEOL = new Array<char>() {'.', ',', ' '};
                var sourceString = line;

                do
                {
                    for (int i = maxLineLength; i >= 1; i--) {
                        if (charsForEOL.Contains(sourceString[i])) {
                            result.Add(sourceString.Substring(0, i)); //здесь был перенос в конце строки
                            sourceString = sourceString.Substring(i + 1);

                            if (sourceString.Length <= maxLineLength) {
                                result.Add(sourceString);
                            }
                            break;
                        }
                        if (i == 1) {
                            result[result.Count - 1] += " " + sourceString.Substring(0, maxLineLength);
                            sourceString = sourceString.Substring(maxLineLength + 1);
                        }
                    }
                } while(sourceString.Length > maxLineLength);
            }
        }
        
        return result;
    }
    
    //возвращает название всех файлов в папке /saves/
    public static Array<string> GetSaveFiles()
    {
        var files = new Array<string>();
        var dir = new Directory();
        dir.Open("user://");

        if (!dir.DirExists("user://saves/"))
        {
            Error result = dir.MakeDir("user://saves/");
            return new Array<string>();
        }
        
        dir.Open("user://saves/");
        dir.ListDirBegin();

        while (true)
        {
            string file = dir.GetNext();
            if (file == "") break;
            if (!file.BeginsWith("."))
            {
                files.Add("user://saves/" + file);
            }
        }
        
        dir.ListDirEnd();
        return files;
    }

    private static FileTableLine? GetFileMetadata(string fileName)
    {
        var file = new File();
        file.OpenCompressed(fileName, File.ModeFlags.Read);
        
        if (!file.IsOpen() || file.EofReached())
        {
            return null;
        }
        
        string name = file.GetLine();
        string date = file.GetLine();
        string level = file.GetLine();
        file.Close();
        return new FileTableLine(name, date, level);
    }

    public static void DeleteSaveFile(string fileName)
    {
        new Directory().Remove("user://saves/" + fileName);
    }
}

public enum Race {
    Earthpony,
    Unicorn,
    Pegasus
}