using Godot;

public class TalkState(
    NavigationMovingController movingController,
    PonyBody body
) : INpcState
{
    public void Enable(NPC npc)
    {
        if (npc.Velocity.Length() > 0) movingController.Stop();
        body?.SetLookTarget(npc.tempVictim);
    }

    public void Update(NPC npc, float delta)
    {
        if (IsStandingOnFoot())
        {
            LookAtTarget(npc);
        }
    }
    
    private void LookAtTarget(NPC npc)
    {
        Vector3 targetPos = npc.tempVictim.GlobalTransform.origin;
        targetPos.y = npc.GlobalTransform.origin.y;
        npc.LookAt(targetPos, Vector3.Up);
    }
    
    private bool IsStandingOnFoot() => body == null || body.IdleAnim is Character.IDLE_ANIM or Character.IDLE_ANIM1;
}
