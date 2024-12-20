﻿using Godot;

public class DragonAttackState(
    StateMachine stateMachine,
    BaseMovingController movingController,
    DragonBody body
) : INpcState
{
    private const float SMASH_DISTANCE = 4;
    private const float FIRE_DISTANCE = 50;
    private const float SMASH_ATTACK_CHANCE = 0.55f;
    
    private bool isSmashAttack;
    private float damageTimer = 3f;
    
    private float fireTimer = 3.5f;
    private float fireWaitTimer = 1;
    
    public void Enable(NPC npc)
    {
        var rand = new RandomNumberGenerator();
        rand.Randomize();
        isSmashAttack = rand.Randf() < SMASH_ATTACK_CHANCE;
        damageTimer = isSmashAttack ? 0.5f : 3;
    }

    public void Update(NPC npc, float delta)
    {
        if (npc.tempVictim == null || !Object.IsInstanceValid(npc.tempVictim) || npc.tempVictim.Health <= 0)
        {
            stateMachine.SetState(SetStateEnum.Idle);
            return;
        }
        
        var enemyDistance = body.GetEnemyDistance();
        var newHeight = isSmashAttack ? 8 : 16;
        
        if (enemyDistance is > 0 and < 60)
        {
            movingController.UpdateHeight(npc.BaseSpeed, npc.tempVictim.Translation.y + newHeight);
        }
        else
        {
            movingController.UpdateHeight(npc.BaseSpeed / 4f, DragonBody.IDLE_FLY_HEIGHT);
        }
        
        var distance = isSmashAttack ? SMASH_DISTANCE : FIRE_DISTANCE;
        movingController.MoveTo(npc.tempVictim.GlobalTransform.origin, distance);
        
        if (!movingController.CloseToPoint)
        {
            return;
        }

        if (isSmashAttack)
        {
            stateMachine.SetState(SetStateEnum.Idle);
            return;
        }

        UpdateFiringAttack(npc, enemyDistance, delta);
    }

    private void UpdateFiringAttack(NPC npc, float enemyDistance, float delta)
    {
        if (enemyDistance > FIRE_DISTANCE)
        {
            movingController.CloseToPoint = false;
            body.SetFireOn(false);
            return;
        }

        if (fireWaitTimer > 0)
        {
            body.Stop();
            movingController.Stop();
            LookAtEnemy(npc);
            fireWaitTimer -= delta;
            return;
        }

        if (IsFiring())
        {
            fireTimer -= delta;
            body.SetFireOn(true);
                            
            LookAtEnemy(npc);

            if (!(enemyDistance > 0)) return;
                        
            if (damageTimer > 0)
            {
                damageTimer -= delta;
            }
            else
            {
                npc.tempVictim.TakeDamage(npc, body.GetDamage(DragonBody.FIRE_DAMAGE));
                damageTimer = 0.1f;
            }

            return;
        }

        body.SetFireOn(false);
        fireWaitTimer = 1;
        fireTimer = 3.5f;
        stateMachine.SetState(SetStateEnum.Idle);
    }
    
    private void LookAtEnemy(NPC npc)
    {
        if (npc.tempVictim == null || !Object.IsInstanceValid(npc.tempVictim))  return;

        var pos = npc.tempVictim.GlobalTransform.origin;
        pos.y = npc.GlobalTranslation.y;
        npc.LookAt(pos, Vector3.Up);
    }
    
    private bool IsFiring() => fireTimer > 0;
}
