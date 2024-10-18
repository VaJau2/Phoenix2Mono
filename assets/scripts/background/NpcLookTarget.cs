using Godot;

public class NpcLookTarget : Spatial
{
    [Export] private NodePath npcPath;
    
    public override void _Ready()
    {
        var npc = GetNode<NPC>(npcPath);
        var body = npc.GetNodeOrNull<PonyBody>("body");
        body?.SetDefaultLookTarget(this);
    }
}
