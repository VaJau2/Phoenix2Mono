using System;
using System.Linq;
using Godot;
using Godot.Collections;

public class NpcSaving(NPC npc)
{
    private readonly string[] skipSignals = {"tree_entered", "tree_exiting"};
    
    public void LoadData(Dictionary data)
    {
        var tempVictimName = data["tempVictim"].ToString();
        
        if (tempVictimName != "")
        {
            var scene = npc.GetNode("/root/Main/Scene");
            npc.tempVictim = Global.FindNodeInScene(scene, tempVictimName) as Character;
        }
        
        npc.relation = (Relation)Enum.Parse(typeof(Relation), data["relation"].ToString());
        npc.aggressiveAgainstPlayer = Convert.ToBoolean(data["aggressiveAgainstPlayer"]);
        npc.IsImmortal = Convert.ToBoolean(data["isImmortal"]);
        npc.myStartPos = data["myStartPos"].ToString().ParseToVector3();
        npc.myStartRot = data["myStartRot"].ToString().ParseToVector3();
        npc.dialogueCode = data["dialogueCode"]?.ToString();
        npc.subtitlesCode = data["subtitlesCode"]?.ToString();
        npc.ignoreDamager = Convert.ToBoolean(data["ignoreDamager"]);
        
        npc.ChestHandler.LoadData(data);

        if (data["signals"] is Godot.Collections.Array signals)
        {
            foreach (Dictionary signalData in signals)
            {
                var signalName = signalData["signal"].ToString();
                var method = signalData["method"].ToString();
                var binds = signalData["binds"] as Godot.Collections.Array;
                
                var targetPath = signalData["target_path"].ToString();
                var target = npc.GetNodeOrNull(targetPath);
                if (target == null) continue;

                if (!npc.IsConnected(signalName, target, method))
                {
                    npc.Connect(signalName, target, method, binds);
                }
            }
        }

        if (data.Contains("showObjects") && data["showObjects"] is Dictionary showObjects)
        {
            foreach (string objectPath in showObjects.Keys)
            {
                bool showObject = Convert.ToBoolean(showObjects[objectPath]);
                npc.SetObjectActive(objectPath, showObject);
            }
        }

        if (npc.Health <= 0)
        {
            npc.MakeDead(false);
            return;
        }
        
        if (!data.Contains("followTarget") || data["followTarget"] == null) return;
        
        LoadFollowTarget(data);
    }

    private async void LoadFollowTarget(Dictionary data)
    {
        await npc.ToSignal(npc.GetTree(), "idle_frame");
        var newFollowTarget = npc.GetNode<Character>(data["followTarget"].ToString());
        npc.SetFollowTarget(newFollowTarget);
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["tempVictim"] = Godot.Object.IsInstanceValid(npc.tempVictim) ? npc.tempVictim.Name : "";
        
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

        var signals = new Godot.Collections.Array();
        foreach (var signal in npc.GetSignalList())
        {
            if (signal is not Dictionary signalDict) continue;

            var connectionList = npc.GetSignalConnectionList(signalDict["name"].ToString());
            if (connectionList == null || connectionList.Count == 0) continue;

            foreach (var connectionData in connectionList)
            {
                if (connectionData is not Dictionary connectionDict) continue;
                if (connectionDict["target"] is not Node target) continue;
                var signalName = signalDict["name"].ToString();
                if (skipSignals.Contains(signalName)) continue;

                signals.Add(new Dictionary
                {
                    {"signal", signalName},
                    {"method", connectionDict["method"].ToString()},
                    {"target_path", target.GetPath()},
                    {"binds", connectionDict["binds"]}
                });
            }
        }

        saveData["signals"] = signals;

        return DictionaryHelper.Merge(saveData, npc.ChestHandler.GetSaveData());
    }
}
