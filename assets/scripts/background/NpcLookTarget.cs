using Godot;

public partial class NpcLookTarget : Node3D
{
    [Export] private NodePath npcPath;
    
    public override void _Ready()
    {
        var npc = GetNode<NPC>(npcPath);
        var body = npc.GetNodeOrNull<PonyBody>("body");
        body?.SetDefaultLookTarget(this);
    }
}
