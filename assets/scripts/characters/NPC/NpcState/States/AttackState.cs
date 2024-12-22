public class AttackState(
    SeekArea seekArea,
    NpcWeapons weapons,
    NavigationMovingController movingController,
    PonyBody body,
    StateMachine stateMachine
) : AbstractNpcState
{
    public override void Enable(NPC npc)
    {
        base.Enable(npc);
        
        if (npc.tempVictim is Player player)
        {
            player.Stealth.AddAttackEnemy(npc);
        }
        
        if (body != null)
        {
            body.CustomIdleAnim = Character.IDLE_ANIM1;
        }

        seekArea.MakeAlliesAttack();
        weapons?.SetWeaponOn(true);
        body?.SetLookTarget(npc.tempVictim);
    }

    public override void _Process(float delta)
    {
        if (tempNpc.tempVictim.Health <= 0)
        {
            tempNpc.SetState(SetStateEnum.Idle);
            return;
        }
        
        AttackEnemy(tempNpc);
    }

    private void AttackEnemy(NPC npc)
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
        
        movingController.GoTo(victimPos, shootDistance / 2f);
        movingController.updatePath = npc.tempVictim.Velocity.Length() > Character.MIN_WALKING_SPEED;
        
        if (movingController.cameToPlace && tempDistance < shootDistance)
        {
            weapons.MakeShoot(tempDistance);
            body?.LookAtTarget();
            movingController.Stop(true);
        }
    }
}
