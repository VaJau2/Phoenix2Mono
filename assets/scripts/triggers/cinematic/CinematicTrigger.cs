using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public class CinematicTrigger : ActivateOtherTrigger
{
    [Export] private NodePath movePath;
    [Export] private NodePath rotatePath;
    [Export] private NodePath returnPath;

    private MoveCinematic moveCinematic;
    private RotateCinematic rotateCinematic;
    
    private readonly List<PathBase> cinematicList = [];
    private int finishedCinematics;
    private bool isCinematicsEnabled;
    
    private Cutscene cutscene;
    
    public override void _Ready()
    {
        base._Ready();
        cutscene = GetNode<Cutscene>("../");
        
        InitCinematic<ReturnCinematic>(returnPath);
        
        if (cinematicList.Count > 0) return;

        moveCinematic = InitCinematic<MoveCinematic>(movePath);
        rotateCinematic = InitCinematic<RotateCinematic>(rotatePath);

        if (moveCinematic != null && rotateCinematic != null)
        {
            moveCinematic.Connect(
                nameof(PathBase.Finished), 
                rotateCinematic,
                nameof(rotateCinematic.OnFinished)
            );
        }
        else
        {
            rotateCinematic?.Connect(
                nameof(PathBase.Finished), 
                rotateCinematic, 
                nameof(rotateCinematic.OnFinished)
            );
        }
    }

    public override void _on_activate_trigger()
    {
        cutscene.AddQueue(this);
    }

    public override void SetActive(bool value)
    {
        base.SetActive(value);
        if (IsActive) _on_activate_trigger();
    }

    public void StartCinematics()
    {
        if (!IsActive) return;

        finishedCinematics = 0;
        
        foreach (var cinematic in cinematicList)
        {
            cinematic.Connect(nameof(PathBase.Finished), this, nameof(OnCinematicFinished));
            cinematic.Enable();
        }
        
        isCinematicsEnabled = true;
        base._on_activate_trigger();
    }

    private T InitCinematic<T>(NodePath path) where T : class
    {
        if (path == null) return null;

        var cinematic = GetNode<PathBase>(path);
        cinematicList.Add(cinematic);
        return cinematic as T;
    }

    public void Skip()
    {
        foreach (var cinematic in cinematicList)
        {
            cinematic.Disable();
        }
    }

    private void OnCinematicFinished(PathBase cinematic)
    {
        cinematic.Disconnect(nameof(PathBase.Finished), this, nameof(OnCinematicFinished));
        finishedCinematics++;

        if (finishedCinematics != cinematicList.Count) return;
        
        isCinematicsEnabled = false;
        cutscene.OnTriggerFinished();
        DeleteTrigger();
    }

    public override Dictionary GetSaveData()
    {
        var saveData = base.GetSaveData();
        saveData["isCinematicsEnabled"] = isCinematicsEnabled;
        saveData["finishedCinematics"] = finishedCinematics;

        for (var i = 0; i < cinematicList.Count; i++)
        {
            saveData[$"isCinematic{i}Enabled"] = cinematicList[i].IsPhysicsProcessing();
        }
        
        return saveData;
    }

    public override void LoadData(Dictionary data)
    {
        base.LoadData(data);
        
        if (!data.Contains("isCinematicsEnabled")) return;
        isCinematicsEnabled = Convert.ToBoolean(data["isCinematicsEnabled"]);
        
        if (!isCinematicsEnabled) return;
        
        for (var i = 0; i < cinematicList.Count; i++)
        {
            if (!Convert.ToBoolean(data[$"isCinematic{i}Enabled"])) continue;
            cinematicList[i].Connect(nameof(PathBase.Finished), this, nameof(OnCinematicFinished));
        }
        
        finishedCinematics = Convert.ToInt32(data["finishedCinematics"]);
    }

    protected override void DeleteTrigger()
    {
        if (finishedCinematics != cinematicList.Count) return;
        base.DeleteTrigger();
    }
}
