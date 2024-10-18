using Godot.Collections;

public class NpcStateDependencies : IDependencies
{
    private NPC npc;

    public NpcStateDependencies(NPC npc)
    {
        this.npc = npc;
    }
    
    public Dictionary GetDictionary()
    {
        return new Dictionary
        {
            { nameof(NpcWeapons), npc.GetNodeOrNull<NpcWeapons>("weapons") },
            { nameof(SeekArea), npc.GetNodeOrNull<SeekArea>("seekArea") },
            { nameof(NpcCovers), npc.GetNodeOrNull<NpcCovers>("covers") },
            { nameof(NpcPatroling), npc.GetNodeOrNull<NpcPatroling>("patroling") },
            
            { nameof(BaseMovingController), npc.GetNodeOrNull<BaseMovingController>("movingController") },
            { nameof(NavigationMovingController), npc.GetNodeOrNull<NavigationMovingController>("movingController") },
            
            { nameof(PonyBody), npc.GetNodeOrNull<PonyBody>("body") },
            { nameof(RoboEyeBody),  npc.GetNodeOrNull<RoboEyeBody>("body") },
            { nameof(DragonBody), npc.GetNodeOrNull<DragonBody>("body") },
            { nameof(DragonMouth), npc.GetNodeOrNull<DragonMouth>("smash-area") },
           
            { nameof(StateMachine), npc.GetNodeOrNull<StateMachine>("stateMachine") }
        };
    }
}
