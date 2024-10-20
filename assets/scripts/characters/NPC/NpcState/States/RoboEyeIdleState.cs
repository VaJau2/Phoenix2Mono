public class RoboEyeIdleState(
    RoboEyeBody body,
    NavigationMovingController movingController,
    NpcPatroling patroling,
    SeekArea seekArea
) : INpcState
{
    public void Enable(NPC npc)
    {
        npc.subtitlesCode = null;
        seekArea.SetActive(true);
        body.IdleAnim = "idle";
        body.ChangeMaterial();
    }

    public void Update(NPC npc, float delta)
    {
        if (patroling.IsEmpty)
        {
            GoToStartPoint(npc);
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
        if (!movingController.cameToPlace)
        {
            movingController.GoTo(npc.myStartPos, movingController.ComeDistance, false);
        } 
    }
}
