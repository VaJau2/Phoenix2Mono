using Godot.Collections;

public class NpcStateDependencies(NPC npc) : IDependencies
{
    public Dictionary GetDictionary()
    {
        return new Dictionary
        {
            { typeof(Player), Global.Get().player },
            { typeof(NPCWeapons), npc.GetNode<NPCWeapons>("weapons") },
            { typeof(SeekArea), npc.GetNode<SeekArea>("seekArea") },
            { typeof(NPCCovers), npc.GetNode<NPCCovers>("covers") },
            
            { typeof(BaseMovingController), npc.GetNode<BaseMovingController>("movingController") },
            { typeof(NavigationMovingController), npc.GetNode<NavigationMovingController>("movingController") },
            
            { typeof(PonyBody), npc.GetNode<PonyBody>("body") },
            { typeof(RoboEyeBody),  npc.GetNode<RoboEyeBody>("body") },
            { typeof(DragonBody), npc.GetNode<DragonBody>("body") },
            { typeof(DragonMouth), npc.GetNode<DragonMouth>("smash-area") },
           
            { typeof(StateMachine), npc.GetNode<StateMachine>("stateMachine") }
        };
    }
}
