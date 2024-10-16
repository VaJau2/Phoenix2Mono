public class RoboEyeAttackState(
    RoboEyeBody body,
    NavigationMovingController movingController,
    StateMachine stateMachine
) : INpcState
{
    public void Enable(NPC npc)
    {
        body.ChangeMaterial(RoboEyeMaterial.Red);
        
        if (npc.tempVictim.Health <= 0) 
        {
            stateMachine.SetState(SetStateEnum.Idle);
            return;
        }
        
        npc.EmitSignal(nameof(NPC.FoundEnemy));
        movingController.Stop();
    }

    public void Update(NPC npc, float delta) { }
}
