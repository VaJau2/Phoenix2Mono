using Godot;

public partial class TalkState(
    NavigationMovingController movingController,
    PonyBody body
) : AbstractNpcState
{
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        if (npc.Velocity.Length() > 0) movingController.Stop();
        body?.SetLookTarget(npc.tempVictim);
    }

    public override void _Process(double delta)
    {
        if (tempNpc.tempVictim == null)
        {
            return;
        }
        
        if (IsStandingOnFoot())
        {
            LookAtTarget(tempNpc);
        }
    }
    
    private void LookAtTarget(NPC npc)
    {
        Vector3 targetPos = npc.tempVictim.GlobalTransform.Origin;
        targetPos.Y = npc.GlobalTransform.Origin.Y;
        npc.LookAt(targetPos, Vector3.Up);
    }
    
    private bool IsStandingOnFoot() => body == null || body.IdleAnim is Character.IDLE_ANIM or Character.IDLE_ANIM1;
}
