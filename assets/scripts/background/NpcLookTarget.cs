using Godot;

public class NpcLookTarget : Spatial
{
    [Export] private NodePath npcPath;
    
    public override void _Ready()
    {
        var npc = GetNode<Pony>(npcPath);
        npc.body.SetDefaultLookTarget(this);
    }
}
