using Godot;

public class DragonIdleState(
    NpcPatroling patroling,
    DragonBody body,
    StateMachine stateMachine,
    BaseMovingController movingController,
    DragonMouth mouth
) : INpcState
{
    private const float ATTACK_COOLDOWN = 1;
    
    private float idleTimer = ATTACK_COOLDOWN;
    
    public void Enable(NPC npc)
    {
        body.SetFireOn(false);
        idleTimer = ATTACK_COOLDOWN;
    }

    public void Update(NPC npc, float delta)
    {
        movingController.UpdateHeight(npc.BaseSpeed / 4f, DragonBody.IDLE_FLY_HEIGHT);
        UpdatePatrolPoints();

        if (mouth.HasEnemy) return;
        
        var enemyDistance = body.GetEnemyDistance();
        UpdateIdleTimer(delta, enemyDistance > 0);
    }
    
    private void UpdatePatrolPoints()
    {
        movingController.MoveTo(patroling.CurrentPatrolPoint, 20);
        
        if (movingController.CloseToPoint)
        {
            patroling.NextPatrolPoint();
        }
    }
    
    private void UpdateIdleTimer(float delta, bool seeEnemy)
    {
        if (idleTimer > 0)
        {
            idleTimer -= delta;
        }
        else
        {
            idleTimer = 1f;
            
            if (seeEnemy)
            {
                stateMachine.SetState(SetStateEnum.Attack);
            }
        }
    }
}
