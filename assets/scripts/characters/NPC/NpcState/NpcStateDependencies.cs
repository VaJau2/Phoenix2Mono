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
            { nameof(NPCWeapons), npc.GetNodeOrNull<NPCWeapons>("weapons") },
            { nameof(SeekArea), npc.GetNodeOrNull<SeekArea>("seekArea") },
            { nameof(NPCCovers), npc.GetNodeOrNull<NPCCovers>("covers") },
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
