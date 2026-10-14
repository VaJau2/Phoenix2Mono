public class RoboEyeAttackState(
    RoboEyeBody body,
    NavigationMovingController movingController,
    StateMachine stateMachine
) : AbstractNpcState
{
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        body.ChangeMaterial(RoboEyeMaterial.Red);
        
        if (npc.tempVictim.Health <= 0) 
        {
            stateMachine.SetState(SetStateEnum.Idle);
            return;
        }
        
        npc.EmitSignal(nameof(NPC.FoundEnemy));
        movingController.Stop();
    }

    public override void _Process(float delta) { }
}
