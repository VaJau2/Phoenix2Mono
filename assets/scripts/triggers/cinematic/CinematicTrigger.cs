using System.Collections.Generic;
using Godot;

public class CinematicTrigger : ActivateOtherTrigger
{
    [Export] private NodePath movePath;
    [Export] private NodePath rotatePath;
    [Export] private NodePath returnPath;

    private readonly List<PathBase> cinematicList = [];
    
    public override void _Ready()
    {
        InitCinematic(returnPath);
        
        if (cinematicList.Count > 0) return;
        
        InitCinematic(movePath);
        InitCinematic(rotatePath);
    }

    public override void _on_activate_trigger()
    {
        if (!IsActive) return;
        
        foreach (var path in cinematicList)
        {
            path.Enable();
        }
    }

    private void InitCinematic(NodePath path)
    {
        if (path == null) return;

        var cinematic = GetNodeOrNull<PathBase>(path);

        if (cinematic == null) return;
        
        cinematicList.Add(cinematic);
    }
}
