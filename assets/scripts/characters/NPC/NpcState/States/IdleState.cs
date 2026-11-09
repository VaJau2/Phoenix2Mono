using Godot;

public partial class IdleState(
    NpcCovers covers,
    NpcWeapons weapons,
    NavigationMovingController movingController,
    PonyBody body,
    NpcPatroling patroling
) : AbstractNpcState
{
    private const float START_POINT_DISTANCE = 0.1f;

    private bool isTeleported;
    
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        if (weapons is { HasWeapon: true })
        {
            weapons.SetWeaponOn(false);
        }
        
        covers?.StopHidingInCover();

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

    public override void _Process(double delta)
    {
        if (tempNpc.followTarget != null)
        {
            tempNpc.SetState(SetStateEnum.Follow);
            return;
        }
        
        if (tempNpc.tempVictim != null)
        {
            tempNpc.SetState(SetStateEnum.Attack);
            return;
        }

        if (patroling.IsEmpty)
        {
            GoToStartPoint(tempNpc);
            return;
        }

        if (patroling.IsWaiting((float)delta))
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
        //NPC уже поставлен на точку. GoTo меряет расстояние до последней точки пути на навмеше, а точка может быть
        //вне его (кресло): при малом ComeDistance NPC шёл к навмешу и снова телепортировался обратно (B-32)
        if (isTeleported && npc.GlobalPosition.DistanceTo(npc.myStartPos) < START_POINT_DISTANCE) return;

        movingController.GoTo(npc.myStartPos, 0, movingController.RunToPoint);
        
        if (movingController.cameToPlace)
        {
            if (!isTeleported)
            {
                npc.GlobalPosition = npc.myStartPos;
                npc.Rotation = new Vector3(0, npc.myStartRot.Y, 0);
                isTeleported = true;
            }
        }
        else
        {
            isTeleported = false;
        }
    }
}
