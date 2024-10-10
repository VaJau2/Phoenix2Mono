using Godot;

public class RoboEyeIdleState(
    RoboEyeBody body,
    NavigationMovingController movingController,
    NpcPatroling patroling
) : INpcState
{
    public void Enable(NPC npc)
    {
        body.ChangeMaterial();
        if (npc.Health == 0) body.Resurrect();
    }

    public void Update(NPC npc, float delta)
    {
        if (patroling.IsEmpty)
        {
            if (!movingController.cameToPlace)
            {
                movingController.GoTo(npc.myStartPos, movingController.ComeDistance, false);
            } 
            else 
            {
                OnCameToPlace(npc, npc.myStartPos, npc.myStartRot.y);
            }

            return;
        }
        
        if (patroling.IsWaiting(delta))
        {
            return;
        }
        
        if (movingController.cameToPlace)
        {
            patroling.NextPatrolPoint();
        }
        
        movingController.GoTo(patroling.CurrentPatrolPoint, 0, false);
    }
    
    private static void OnCameToPlace(NPC npc, Vector3 pos, float angle)
    {
        npc.GlobalTransform = Global.SetNewOrigin(npc.GlobalTransform, pos);
        npc.Rotation = new Vector3
        (
            npc.Rotation.x,
            angle,
            npc.Rotation.z
        );
    }
}
