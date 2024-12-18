using Godot;
using Godot.Collections;

public class SearchState(
    StateMachine stateMachine,
    NpcWeapons weapons,
    NavigationMovingController movingController,
    PonyBody body
) : AbstractNpcState, ISavable
{
    private const float SEARCH_TIMER = 12f;

    private Vector3 lastSeePos;
    private float searchTimer;

    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        searchTimer = SEARCH_TIMER;

        if (!IsInstanceValid(npc.tempVictim))
        {
            //врага нет, позиции нет, искать нечего
            stateMachine.SetState(SetStateEnum.Idle);
            return;
        }

        if (npc.tempVictim is Player player)
        {
            player.Stealth.AddSeekEnemy(npc);
        }

        lastSeePos = npc.tempVictim.GlobalTranslation;
        
        body?.SetLookTarget(null);
    }

    public override void _Process(float delta)
    {
        if (searchTimer > 0 && weapons.HasWeapon)
        {
            searchTimer -= delta;
        }
        else
        {
            tempNpc.SetState(SetStateEnum.Idle);
            return;
        }
                
        if (tempNpc.BaseSpeed == 0)
        {
            tempNpc.LookAt(lastSeePos, Vector3.Up);
            return;
        }
        
        movingController.GoTo(lastSeePos);
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
