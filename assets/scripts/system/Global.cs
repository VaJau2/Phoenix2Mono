using System.Collections;
using Godot;
using Godot.Collections;
using System.Globalization;
using System.Collections.Generic;

//Синглтон-класс для управления паузами и для всяких универсальных методов
public partial class Global 
{
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
    public string autosaveName;
    
    //массив файлов сохранений
    public static List<FileTableLine> saveFilesArray = new();
    public static Array<string> deletedObjects { get; private set; } = [];

    private static Dictionary jsonCache;
    private static string jsonCachePath;

    public static void AddDeletedObject(string name)
    {
        if (!name.StartsWith("Created_") && !deletedObjects.Contains(name))
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
                if (fileData is { } line)
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
        return raceString switch
        {
            "earthpony" => Race.Earthpony,
            "pegasus" => Race.Pegasus,
            "unicorn" => Race.Unicorn,
            _ => Race.Earthpony
        };
    }

    public static string RaceToString(Race race)
    {
        return race switch
        {
            Race.Earthpony => "earthpony",
            Race.Pegasus => "pegasus",
            Race.Unicorn => "unicorn",
            _ => "earthpony"
        };
    }

    public static Dictionary LoadJsonFile(string filePath)
    {
        if (jsonCachePath == filePath)
        {
            return jsonCache;
        }
        
        string path = "res://" + filePath;

        var result = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (result.IsOpen()) 
        {
            var textJson = result.GetAsText();
            result.Close();

            var json = new Json();
            var resultJson = json.Parse(textJson);

            if (resultJson == Error.Ok) 
            {  
                jsonCachePath = filePath;
                jsonCache = json.Data.AsGodotDictionary();
                return jsonCache;
            }
            
            GD.PrintErr("parse json (" + filePath + ") error: "
                        + json.GetErrorMessage()
                        + ", in line: "
                        + json.GetErrorLine()
                        );
            return null;
        }
        
        GD.PrintErr("error loading JSON file in: " + path);
        return null;
    }

    public static Transform3D SetNewOrigin(Transform3D transform, Vector3 newOrigin)
    {
        Transform3D tempTrans = transform;
        tempTrans.Origin = newOrigin;
        return tempTrans;
    }

    public SignalAwaiter ToTimer(float time, Node _object = null, bool pauseProcess = false)
    {
        var newTimer = new Timer {Autostart = true, OneShot = true, WaitTime = time};
        if (pauseProcess)
        {
            newTimer.ProcessMode = Node.ProcessModeEnum.Always;
        }

        if (GodotObject.IsInstanceValid(_object))
        {
            _object?.AddChild(newTimer);
        }
        else
        {
            player.GetNode("/root/Main/Scene").AddChild(newTimer);
        }

        newTimer.Timeout += newTimer.QueueFree;
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
        var actions = InputMap.ActionGetEvents(actionName);
        if (actions.Count == 0) return null;
        if (!(actions[0] is InputEventKey action)) return null;
        return OS.GetKeycodeString(action.Keycode);
    }

    public static float ParseFloat(string value) => float.Parse(value, CultureInfo.InvariantCulture);


    //уменьшает длину строки, разбивая длинные строки на подстроки
    //разбивает по точкам, запятым и пробелам
    public static Array<string> ClumpLineLength(Array<string> lines, int maxLineLength)
    {
        Array<string> result = [];
        foreach(string line in lines)
        {
            if (line.Length <= maxLineLength) 
            {
                result.Add(line);
            } 
            else 
            {
                Array<char> charsForEOL = new Array<char>() {'.', ',', ' '};
                var sourceString = line;

                do
                {
                    for (int i = maxLineLength; i >= 1; i--) 
                    {
                        if (charsForEOL.Contains(sourceString[i])) 
                        {
                            result.Add(sourceString.Substring(0, i)); //здесь был перенос в конце строки
                            sourceString = sourceString.Substring(i + 1);

                            if (sourceString.Length <= maxLineLength) 
                            {
                                result.Add(sourceString);
                            }
                            break;
                        }
                        
                        if (i == 1) 
                        {
                            if (result.Count == 0)
                            {
                                result.Add(sourceString.Substring(0, maxLineLength));
                            }
                            else
                            {
                                result[result.Count - 1] += " " + sourceString.Substring(0, maxLineLength);
                            }
                            
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

        if (!DirAccess.DirExistsAbsolute("user://saves/"))
        {
            DirAccess.MakeDirAbsolute("user://saves/");
            return [];
        }
        
        var dir = DirAccess.Open("user://saves/");
        dir.ListDirBegin();

        while (true)
        {
            string file = dir.GetNext();
            if (file == "") break;
            if (!file.StartsWith("."))
            {
                files.Add("user://saves/" + file);
            }
        }
        
        dir.ListDirEnd();
        return files;
    }

    private static FileTableLine? GetFileMetadata(string fileName)
    {
        var file = FileAccess.OpenCompressed(fileName, FileAccess.ModeFlags.Read);
        
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
        DirAccess.RemoveAbsolute("user://saves/" + fileName);
    }
}

public enum Race {
    Earthpony,
    Unicorn,
    Pegasus
}