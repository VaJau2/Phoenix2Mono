public class HidingState(
    NpcWeapons weapons,
    NpcCovers covers,
    NavigationMovingController movingController,
    StateMachine stateMachine,
    PonyBody body
) : AbstractNpcState
{
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        covers.FindCover(npc);
        
        if (!HasGun && body != null)
        {
            body.CustomIdleAnim = "Sit";
        }
    }

    public override void _Process(float delta)
    {
        if (tempNpc.tempVictim.Health <= 0)
        {
            tempNpc.SetState(SetStateEnum.Idle);
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
