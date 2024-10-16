using System;
using Godot;
using Godot.Collections;

public class StateMachine: Node, ISavable
{
    [Export] private Array<SetStateEnum> availableSetStates;
    [Export] private Array<NpcStateEnum> availableNpcStates;
    private NpcStateEnum CurrentStateEnum => NpcStateConverter.ToEnum(currentState);
    
    private INpcState currentState;
    private NPC npc;

    public SetStateEnum GetCurrentSetState()
    {
        return FindSetState(CurrentStateEnum);
    }
    
    public void SetState(SetStateEnum newState, bool checkHealth = true)
    {
        if (!availableSetStates.Contains(newState)) return;
        if (currentState != null && GetCurrentSetState() == newState) return;

        SetState(FindNpcState(newState), checkHealth);
    }

    private void SetState(NpcStateEnum npcState, bool checkHealth = true)
    {
        if (npc.Health <= 0 && checkHealth) return;
        
        var stateType = NpcStateConverter.FromEnum(npcState);
        var dependencies = new NpcStateDependencies(npc);
        currentState = DependencyInjection.CreateClass<INpcState>(stateType, dependencies);
        currentState.Enable(npc);
    }
    
    public override void _Ready()
    {
        if (availableNpcStates.Count != availableSetStates.Count)
        {
            GD.PrintErr("availableSetStates must be the same size as availableNpcStates");    
        }
        
        npc = GetParent<NPC>();

        SetState(SetStateEnum.Idle, false);
    }

    public override void _Process(float delta)
    {
        if (npc.Health <= 0)
        {
            return;
        }
        
        currentState.Update(npc, delta);
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["state"] = GetCurrentSetState();

        if (currentState is ISavable savableState)
        {
            var stateData = savableState.GetSaveData();
            if (stateData.Count > 0)
            {
                saveData["stateData"] = savableState.GetSaveData();
            }
        }
        
        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        var newState = (SetStateEnum)Enum.Parse(typeof(SetStateEnum), data["state"].ToString());
        SetState(newState);

        if (currentState is ISavable savableState && data.Contains("stateData"))
        {
            savableState.LoadData((Dictionary)data["stateData"]);
        }
    }

    private NpcStateEnum FindNpcState(SetStateEnum setState)
    {
        for (var i = 0; i < availableSetStates.Count; i++)
        {
            if (setState == availableSetStates[i])
            {
                return availableNpcStates[i];
            }
        }

        throw new Exception($"Didnt find available npc state for {setState}");
    }
    
    private SetStateEnum FindSetState(NpcStateEnum npcState)
    {
        for (var i = 0; i < availableNpcStates.Count; i++)
        {
            if (npcState == availableNpcStates[i])
            {
                return availableSetStates[i];
            }
        }

        throw new Exception($"Didnt find available set state for {npcState}");
    }
}

public enum SetStateEnum
{
    Idle,
    Search,
    Attack,
    Hiding,
    Talk,
    Disabled
}
