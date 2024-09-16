using System.Collections.Generic;
using Godot;

public class CinematicTrigger : ActivateOtherTrigger
{
    [Export] private NodePath movePath;
    [Export] private NodePath rotatePath;

    private readonly List<PathBase> cinematicList = [];
    
    public override void _Ready()
    {
        InitCinematic(movePath);
        InitCinematic(rotatePath);

        if (cinematicList.Count > 0) return;

        InitReturnToPlayer();
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

    private void InitReturnToPlayer()
    {
        var prefab = GD.Load<PackedScene>("res://objects/cinematic/ReturnToPlayer.tscn");
        var returnToPlayer = (PathBase)prefab.Instance();
        AddChild(returnToPlayer);
        cinematicList.Add(returnToPlayer);
    }
}
