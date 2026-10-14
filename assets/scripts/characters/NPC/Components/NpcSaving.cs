using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class NpcSaving(NPC npc)
{
    private readonly string[] skipSignals = ["tree_entered", "tree_exiting"];
    
    public void LoadData(Dictionary data)
    {
        var tempVictimName = data["tempVictim"].ToString();
        
        if (tempVictimName != "")
        {
            var scene = npc.GetNode("/root/Main/Scene");
            npc.tempVictim = Global.FindNodeInScene(scene, tempVictimName) as Character;
        }
        
        npc.relation = (Relation)Enum.Parse(typeof(Relation), data["relation"].ToString());
        npc.aggressiveAgainstPlayer = data["aggressiveAgainstPlayer"].AsBool();
        npc.IsImmortal = data["isImmortal"].AsBool();
        npc.myStartPos = data["myStartPos"].ToString().ParseToVector3();
        npc.myStartRot = data["myStartRot"].ToString().ParseToVector3();
        npc.dialogueCode = data["dialogueCode"].AsStringOrNull();
        npc.subtitlesCode = data["subtitlesCode"].AsStringOrNull();
        npc.ignoreDamager = data["ignoreDamager"].AsBool();
        
        npc.ChestHandler.LoadData(data);

        if (data["patrolArray"].AsArrayOrNull() is Godot.Collections.Array patrolArray)
        {
            var pathArray = new Array<NodePath>();
            foreach (string item in patrolArray)
            {
                pathArray.Add(item);
            }
            
            npc.patrolArray = pathArray;
        }

        if (data["signals"].AsArrayOrNull() is Godot.Collections.Array signals)
        {
            foreach (Dictionary signalData in signals)
            {
                var signalName = signalData["signal"].ToString();
                var method = signalData["method"].ToString();
                var binds = signalData["binds"].AsArrayOrNull();
                
                var flags = signalData.ContainsKey("flags") ? signalData["flags"].AsUInt32() : 0;

                var targetPath = signalData["target_path"].ToString();
                var target = npc.GetNodeOrNull(targetPath);
                if (target == null) continue;

                if (binds is { Count: > 0 })
                {
                    if (!BoundCall.IsConnected(npc, signalName, target, method))
                    {
                        BoundCall.Connect(npc, signalName, target, method, binds, flags);
                    }
                }
                else if (!npc.IsConnected(signalName, new Callable(target, method)))
                {
                    npc.Connect(signalName, new Callable(target, method), flags);
                }
            }
        }

        if (data.ContainsKey("showObjects") && data["showObjects"].AsDictionaryOrNull() is Dictionary showObjects)
        {
            foreach (string objectPath in showObjects.Keys)
            {
                bool showObject = showObjects[objectPath].AsBool();
                npc.SetObjectActive(objectPath, showObject);
            }
        }

        if (npc.Health <= 0)
        {
            npc.MakeDead(false);
            return;
        }
        
        if (!data.ContainsKey("followTarget") || data["followTarget"].IsNull()) return;
        
        LoadFollowTarget(data);
    }

    private void LoadFollowTarget(Dictionary data)
    {
        var newFollowTarget = npc.GetNode<Character>(data["followTarget"].ToString());
        npc.SetFollowTarget(newFollowTarget);
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["tempVictim"] = GodotObject.IsInstanceValid(npc.tempVictim) ? npc.tempVictim.Name : "";
        
        saveData["relation"] = npc.relation.ToString();
        saveData["aggressiveAgainstPlayer"] = npc.aggressiveAgainstPlayer;
        saveData["isImmortal"] = npc.IsImmortal;
        saveData["myStartPos"] = npc.myStartPos;
        saveData["myStartRot"] = npc.myStartRot;
        saveData["dialogueCode"] = npc.dialogueCode;
        saveData["subtitlesCode"] = npc.subtitlesCode;
        saveData["showObjects"] = npc.objectsChangeActive;
        saveData["ignoreDamager"] = npc.ignoreDamager;
        saveData["followTarget"] = npc.followTarget?.GetPath();
        saveData["patrolArray"] = npc.patrolArray;

        var signals = new Godot.Collections.Array();
        foreach (var signal in npc.GetSignalList())
        {
            if (signal is not Dictionary signalDict) continue;

            var connectionList = npc.GetSignalConnectionList(signalDict["name"].ToString());
            if (connectionList == null || connectionList.Count == 0) continue;

            foreach (var connectionDict in connectionList)
            {
                //в Godot 4 вместо target/method/binds подключение хранит Callable,
                //а подключения с аргументами идут через BoundCall
                var callable = connectionDict["callable"].AsCallable();
                var boundCall = callable.Target as BoundCall;
                var target = boundCall != null ? boundCall.Target : callable.Target as Node;
                if (target == null || target == npc) continue;
                var signalName = signalDict["name"].ToString();
                if (skipSignals.Contains(signalName)) continue;

                signals.Add(new Dictionary
                {
                    {"signal", signalName},
                    {"method", boundCall != null ? boundCall.Method : callable.Method},
                    {"target_path", target.GetPath()},
                    {"binds", boundCall != null ? boundCall.Binds : new Godot.Collections.Array()},
                    {"flags", connectionDict["flags"]}
                });
            }
        }

        saveData["signals"] = signals;

        return DictionaryHelper.Merge(saveData, npc.ChestHandler.GetSaveData());
    }
}
