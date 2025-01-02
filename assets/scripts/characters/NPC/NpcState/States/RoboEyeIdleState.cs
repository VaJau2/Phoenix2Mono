using Godot;

public class RoboEyeIdleState(
    RoboEyeBody body,
    NavigationMovingController movingController,
    NpcPatroling patroling,
    SeekArea seekArea
) : AbstractNpcState
{
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        npc.subtitlesCode = null;
        seekArea.SetActive(true);
        body.IdleAnim = "idle";
        body.ChangeMaterial();
    }

    public override void _Process(float delta)
    {
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
            tempNpc.GlobalRotation = patroling.CurrentPatrolRotation;
            patroling.NextPatrolPoint();
        }
    }

    private void GoToStartPoint(NPC npc)
    {
        movingController.GoTo(npc.myStartPos, 0, movingController.RunToPoint);
        
        if (movingController.cameToPlace)
        {
            npc.GlobalTranslation = npc.myStartPos;
            npc.Rotation = new Vector3(0, npc.myStartRot.y, 0);
        }
    }
}
