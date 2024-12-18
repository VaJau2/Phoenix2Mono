using Godot;

public class IdleState(
    NpcCovers covers,
    NpcWeapons weapons,
    NavigationMovingController movingController,
    PonyBody body,
    NpcPatroling patroling
) : AbstractNpcState
{
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        if (weapons is { HasWeapon: true })
        {
            weapons.SetWeaponOn(false);
            covers?.StopHidingInCover();
        }

        if (npc.tempVictim is Player player)
        {
            player.Stealth.RemoveSeekEnemy(npc);
        }
        
        if (body != null)
        {
            body.CustomIdleAnim = null;
            body.SetLookTarget(null);
        }
        
        npc.tempVictim = null;
    }

    public override void _Process(float delta)
    {
        if (tempNpc.followTarget != null)
        {
            tempNpc.SetState(SetStateEnum.Follow);
            return;
        }

        if (patroling.IsEmpty)
        {
            GoToStartPoint(tempNpc);
            return;
        }

        if (patroling.IsWaiting(delta))
        {
            return;
        }
        
        movingController.GoTo(patroling.CurrentPatrolPoint, 0, false);

        if (movingController.cameToPlace)
        {
            patroling.NextPatrolPoint();
        }
    }

    private void GoToStartPoint(NPC npc)
    {
        movingController.GoTo(npc.myStartPos, 0, movingController.RunToPoint);
        
        if (movingController.cameToPlace)
        {
            npc.GlobalTransform = Global.SetNewOrigin(npc.GlobalTransform, npc.myStartPos);
            npc.Rotation = new Vector3(0, npc.myStartRot.y, 0);
        }
    }
}
