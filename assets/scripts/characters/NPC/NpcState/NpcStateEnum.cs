using System;

public enum NpcStateEnum
{
    Idle,
    Attack,
    Hiding,
    Search,
    Talk,
    
    RoboEyeIdle,
    RoboEyeAttack,
    RoboEyeSearch,
    RoboEyeDisabled,
    
    DragonIdle,
    DragonAttack
}

public static class NpcStateConverter
{
    public static Type FromEnum(NpcStateEnum value)
    {
        return value switch
        {
            NpcStateEnum.Idle => typeof(IdleState),
            NpcStateEnum.Attack => typeof(AttackState),
            NpcStateEnum.Hiding => typeof(HidingState),
            NpcStateEnum.Search => typeof(SearchState),
            NpcStateEnum.Talk => typeof(TalkState),
            
            NpcStateEnum.RoboEyeIdle => typeof(RoboEyeIdleState),
            NpcStateEnum.RoboEyeAttack => typeof(RoboEyeAttackState),
            NpcStateEnum.RoboEyeSearch => typeof(RoboEyeSearchState),
            NpcStateEnum.RoboEyeDisabled => typeof(RoboEyeDisabledState),
            
            NpcStateEnum.DragonIdle => typeof(DragonIdleState),
            NpcStateEnum.DragonAttack => typeof(DragonAttackState),
            
            _ => null
        };
    }

    public static NpcStateEnum ToEnum(INpcState value)
    {
        return value switch
        {
            IdleState => NpcStateEnum.Idle,
            AttackState => NpcStateEnum.Attack,
            HidingState => NpcStateEnum.Hiding,
            SearchState => NpcStateEnum.Search,
            TalkState => NpcStateEnum.Talk,
            
            RoboEyeIdleState => NpcStateEnum.RoboEyeIdle,
            RoboEyeAttackState => NpcStateEnum.RoboEyeAttack,
            RoboEyeSearchState => NpcStateEnum.RoboEyeSearch,
            RoboEyeDisabledState => NpcStateEnum.RoboEyeDisabled,
            
            DragonIdleState => NpcStateEnum.DragonIdle,
            DragonAttackState => NpcStateEnum.DragonAttack,
            _ => NpcStateEnum.Idle
        };
    }
}
