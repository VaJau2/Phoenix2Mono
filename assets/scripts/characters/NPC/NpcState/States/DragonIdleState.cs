public class DragonIdleState(
    NpcPatroling patroling,
    DragonBody body,
    StateMachine stateMachine,
    BaseMovingController movingController,
    DragonMouth mouth
) : INpcState
{
    private const float ATTACK_COOLDOWN = 1;
    
    private float attackTimer = ATTACK_COOLDOWN;
    
    public void Enable(NPC npc)
    {
        attackTimer = ATTACK_COOLDOWN;
    }

    public void Update(NPC npc, float delta)
    {
        body.PlayIdleSounds(delta);
        movingController.UpdateHeight(npc.BaseSpeed / 4f, DragonBody.IDLE_FLY_HEIGHT);
        UpdatePatrolPoints();

        if (mouth.HasEnemy) return;
        
        var enemyDistance = body.GetEnemyDistance();
        UpdateAttackTimer(delta, enemyDistance > 0);
    }
    
    private void UpdatePatrolPoints()
    {
        if (!movingController.CloseToPoint)
        {
            movingController.MoveTo(patroling.CurrentPatrolPoint, 20);
        }
        else
        {
            patroling.NextPatrolPoint();
        }
    }
    
    private void UpdateAttackTimer(float delta, bool seeEnemy)
    {
        if (attackTimer > 0)
        {
            attackTimer -= delta;
        }
        else
        {
            attackTimer = 1f;
            
            if (seeEnemy)
            {
                stateMachine.SetState(SetStateEnum.Attack);
            }
        }
    }
}
