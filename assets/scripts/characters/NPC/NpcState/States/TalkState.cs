using Godot;
using Godot.Collections;

public class TalkState(
    NavigationMovingController movingController,
    PonyBody body
) : INpcState
{
    public void Enable(NPC npc)
    {
        if (npc.Velocity.Length() > 0) movingController.Stop();
        body.SetLookTarget(npc.tempVictim);
    }

    public void Update(NPC npc, float delta)
    {
        if (IsStandingOnFoot())
        {
            LookAtTarget(npc);
        }
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary();
    }

    public void LoadData(Dictionary data) { }
    
    private void LookAtTarget(NPC npc)
    {
        Vector3 targetPos = npc.tempVictim.GlobalTransform.origin;
        targetPos.y = npc.GlobalTransform.origin.y;
        npc.LookAt(targetPos, Vector3.Up);
    }
    
    private bool IsStandingOnFoot() => body.IdleAnim is Character.IDLE_ANIM or Character.IDLE_ANIM1;
}
