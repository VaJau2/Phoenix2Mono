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
    
    public override void _Ready()
    {
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
        if (!IsActive) return;
        
        foreach (var path in cinematicList)
        {
            path.Enable();
        }
    }

    private T InitCinematic<T>(NodePath path) where T : class
    {
        if (path == null) return null;

        var cinematic = GetNode<PathBase>(path);
        cinematicList.Add(cinematic);
        return cinematic as T;
    }
}
