using Godot;

public class IdleState(
    NPCCovers covers,
    NPCWeapons weapons,
    NavigationMovingController movingController,
    PonyBody body,
    NpcPatroling patroling
) : INpcState
{
    private NPC tempNpc;

    public void Enable(NPC npc)
    {
        tempNpc = npc;

        if (weapons is { HasWeapon: true })
        {
            weapons.SetWeaponOn(false);
            covers?.StopHidingInCover();
        }

        if (npc.tempVictim is Player player)
        {
            player.Stealth.RemoveSeekEnemy(npc);
        }
        
        npc.tempVictim = null;
        
        body?.SetLookTarget(null);
        
        if (body != null && body.IdleAnim != Character.IDLE_ANIM1)
        {
            body.IdleAnim = Character.IDLE_ANIM1;
        }
    }

    public void Update(NPC npc, float delta)
    {
        if (npc.followTarget != null)
        {
            FollowTarget();
            return;
        }

        if (patroling.IsEmpty)
        {
            if (!movingController.cameToPlace)
            {
                if (movingController.stopAreaEntered)
                {
                    movingController.Stop(true);
                }
                else
                {
                    movingController.GoTo(npc.myStartPos, 0, false);
                }
            }
            else
            {
                npc.GlobalTransform = Global.SetNewOrigin(npc.GlobalTransform, npc.myStartPos);
                npc.Rotation = new Vector3(0, npc.myStartRot.y, 0);
            }

            return;
        }

        if (patroling.IsWaiting(delta))
        {
            return;
        }

        if (movingController.stopAreaEntered)
        {
            movingController.Stop(true);
            return;
        }
        
        movingController.GoTo(patroling.CurrentPatrolPoint, 0, false);

        if (movingController.cameToPlace)
        {
            patroling.NextPatrolPoint();
        }
    }

    private void FollowTarget()
    {
        var targetPos = tempNpc.followTarget.GlobalTranslation;
        movingController.GoTo(targetPos, movingController.ComeDistance * 2f);
        movingController.updatePath = tempNpc.followTarget?.Velocity.Length() > Character.MIN_WALKING_SPEED;
    }
}
