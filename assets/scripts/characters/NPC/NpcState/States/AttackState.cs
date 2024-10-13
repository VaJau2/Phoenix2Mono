using Godot;

public class AttackState(
    SeekArea seekArea,
    NPCWeapons weapons,
    NavigationMovingController movingController,
    PonyBody body,
    StateMachine stateMachine
) : INpcState
{
    private float shootCooldown;

    public void Enable(NPC npc)
    {
        if (npc.tempVictim is Player player)
        {
            player.Stealth.AddAttackEnemy(npc);
        }

        seekArea.MakeAlliesAttack();
        weapons?.SetWeaponOn(true);
        body?.SetLookTarget(npc.tempVictim);
    }

    public void Update(NPC npc, float delta)
    {
        if (npc.tempVictim.Health <= 0)
        {
            npc.SetState(SetStateEnum.Idle);
            return;
        }
        
        AttackEnemy(npc, delta);
    }

    private void AttackEnemy(NPC npc, float delta)
    {
        if (!weapons.HasWeapon)
        {
            //если нет оружия
            //бегаем по укрытиям и молимся Селестии
            stateMachine.SetState(SetStateEnum.Hiding);
            return;
        }

        var victimPos = npc.tempVictim.GlobalTransform.origin;
        var shootDistance = weapons.GetStatsFloat("shootDistance");
        var tempDistance = npc.GlobalTranslation.DistanceTo(victimPos);
        
        movingController.GoTo(victimPos, shootDistance / 1.5f);
        movingController.updatePath = npc.tempVictim.Velocity.Length() > Character.MIN_WALKING_SPEED;
        
        if (movingController.cameToPlace && tempDistance < shootDistance)
        {
            UpdateShooting(tempDistance, delta);
            body?.LookAtTarget();
            movingController.Stop(true);
        }
    }

    private void UpdateShooting(float victimDistance, float delta)
    {
        if (shootCooldown > 0)
        {
            shootCooldown -= delta;
            return;
        }

        shootCooldown = weapons.MakeShoot(victimDistance);
    }
}
