using System;

// Все эти стейты точно-точно используются неписями
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
        var stateString = value + "State";
        return Type.GetType(stateString);
    }

    public static NpcStateEnum ToEnum(INpcState value)
    {
        var stateName = value
            .GetType()
            .Name
            .Replace("State", "");
        
        return (NpcStateEnum)Enum.Parse(typeof(NpcStateEnum), stateName);
    }
}
