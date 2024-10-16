using System.Collections.Generic;
using Godot;

public class CinematicTrigger : ActivateOtherTrigger
{
    [Export] private NodePath movePath;
    [Export] private NodePath rotatePath;
    [Export] private NodePath returnPath;

    private MoveCinematic moveCinematic;
    private RotateCinematic rotateCinematic;
    
    private readonly List<PathBase> cinematicList = [];
    private int finishedCinematics;
    
    private Cutscene cutscene;
    
    
    public override void _Ready()
    {
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

    public void StartCinematics()
    {
        if (!IsActive) return;

        finishedCinematics = 0;
        
        foreach (var cinematic in cinematicList)
        {
            cinematic.Connect(nameof(PathBase.Finished), this, nameof(OnCinematicFinished));
            cinematic.Enable();
        }
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

        if (finishedCinematics == cinematicList.Count)
        {
            cutscene.OnTriggerFinished();
        }
    }
}
