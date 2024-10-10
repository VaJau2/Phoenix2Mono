﻿using Godot;
using Godot.Collections;

public class SearchState(
    Player player,
    StateMachine stateMachine,
    NPCWeapons weapons,
    NavigationMovingController movingController,
    PonyBody body
) : INpcState, ISavable
{
    private const float SEARCH_TIMER = 12f;

    private Vector3 lastSeePos;
    private float searchTimer;

    public void Enable(NPC npc)
    {
        searchTimer = SEARCH_TIMER;

        if (!Object.IsInstanceValid(npc.tempVictim))
        {
            //врага нет, позиции нет, искать нечего
            stateMachine.SetState(SetStateEnum.Idle);
            return;
        }

        if (npc.tempVictim == player)
        {
            player.Stealth.AddSeekEnemy(npc);
        }

        lastSeePos = npc.tempVictim.GlobalTranslation;
        
        body?.SetLookTarget(null);
    }

    public void Update(NPC npc, float delta)
    {
        if (searchTimer > 0 && weapons.HasWeapon)
        {
            searchTimer -= delta;
        }
        else
        {
            npc.SetState(SetStateEnum.Idle);
            return;
        }
                
        if (movingController.WalkSpeed == 0)
        {
            npc.LookAt(lastSeePos, Vector3.Up);
            return;
        }
        
        if (!movingController.cameToPlace)
        {
            movingController.GoTo(lastSeePos);
        }
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["lastSeePos"] = lastSeePos;
        return saveData;
    }

    public void LoadData(Dictionary data)
    {
        lastSeePos = data["lastSeePos"] as Vector3? ?? default;
    }
}
