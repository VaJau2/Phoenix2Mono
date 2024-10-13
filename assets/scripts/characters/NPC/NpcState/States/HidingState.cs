public class HidingState(
    NPCWeapons weapons,
    NPCCovers covers,
    NavigationMovingController movingController,
    StateMachine stateMachine,
    PonyBody body
) : INpcState
{
    public void Enable(NPC npc)
    {
        covers.FindCover(npc);
        
        if (!HasGun && body != null)
        {
            body.CustomIdleAnim = "Sit";
        }
    }

    public void Update(NPC npc, float delta)
    {
        if (npc.tempVictim.Health <= 0)
        {
            npc.SetState(SetStateEnum.Idle);
            return;
        }

        if (covers.InCover)
        {
            body?.LookAtTarget();
            UpdateCoverTimer(delta);
        }
        else
        {
            RunToCover();
        }
    }

    private void UpdateCoverTimer(float delta)
    {
        if (!HasGun) return;
        
        if (covers.CoverTimer > 0)
        {
            covers.CoverTimer -= delta;
        }
        else
        {
            //идем в атаку
            covers.StopHidingInCover();
            stateMachine.SetState(SetStateEnum.Attack);
        }
    }

    private void RunToCover()
    {
        movingController.GoTo(covers.TempCoverPlace);
        
        if (movingController.cameToPlace)
        {
            covers.InCover = true;
        }
    }

    private bool HasGun => weapons is { HasWeapon: true };
}
