using Godot;
using Godot.Collections;

public class SeekArea : Area
{
    private const float ATTACK_TIME = 3.5f;

    private const float INCREASE_TIMER = 0.3f;
    private const float CROUCH_MULTIPLY = 0.5f;
    private const float WALK_MULTIPLY = 4f;
    private const float LIGHT_MULTIPLY = 4f;
    private const float NPC_MULTIPLY = 10f;
    private const float STEALTH_BUCK_MULTIPLY = 0.01f;

    private Array<Character> enemiesInArea = new();
    private Array<NPC> alliesInArea = new();
    private Array<float> attackTimer = new();
    private int tempEnemy;

    private NPC npc;
    private RayCast ray;

    public void MakeAlliesAttack()
    {
        foreach (var ally in alliesInArea)
        {
            if (!IsInstanceValid(ally) || ally.GetState() != SetStateEnum.Idle) continue;
            
            ally.SeekArea.AddEnemyInArea(npc.tempVictim);
            ally.aggressiveAgainstPlayer = npc.aggressiveAgainstPlayer;
            ally.tempVictim = npc.tempVictim;
            ally.SetState(SetStateEnum.Attack);
        }
    }

    public override void _Ready()
    {
        npc = GetParent<NPC>();
        ray = GetNode<RayCast>("ray");
        ray.AddException(npc);
    }

    public override void _Process(float delta)
    {
        if (ray.Enabled)
        {
            //при поворотах неписей луч может идти не в ту точку, поэтому его поворот нужно сначала сбросить
            Vector3 newRayDegrees = ray.RotationDegrees;
            newRayDegrees.y = -npc.RotationDegrees.y;
            ray.RotationDegrees = newRayDegrees;
        }

        if (npc.GetState() == SetStateEnum.Attack)
        {
            //теряем противника, если не видим его
            if (!SeeCharacter(npc.tempVictim))
            {
                if (CheckHiding())
                {
                    //если мы прячемся, то то, что мы не 
                    //видим его, это норма
                    return;
                }

                npc.SetState(SetStateEnum.Search);
            }
            else
            {
                //если увидели противника, будучи в укрытии
                //больше не прячемся
                if (npc.Covers != null)
                {
                    if (CheckHiding() && npc.Covers.InCover)
                    {
                        npc.Covers.StopHidingInCover();
                    }
                }
            }
        }
        else
        {
            if (enemiesInArea.Count == 0)
            {
                return;
            }

            //если рядом есть враги
            if (tempEnemy < enemiesInArea.Count)
            {
                //проверяем их видимость
                var tempVictim = enemiesInArea[tempEnemy];
                if (tempVictim == null)
                {
                    enemiesInArea.RemoveAt(tempEnemy);
                    return;
                }

                if (SeeCharacter(tempVictim))
                {
                    if (attackTimer[tempEnemy] >= ATTACK_TIME)
                    {
                        //идем в атаку на врага, которого заметили
                        npc.tempVictim = tempVictim;
                        npc.SetState(
                            npc.Weapons is { HasWeapon: true }
                                ? SetStateEnum.Attack
                                : SetStateEnum.Hiding
                        );
                    }
                    else
                    {
                        //считаем таймер видимости
                        attackTimer[tempEnemy] += GetSeeTimerSpeed(tempVictim);
                    }
                }

                tempEnemy++;
            }
            else
            {
                tempEnemy = 0;
            }
        }
    }

    public void AddEnemyInArea(Character enemy)
    {
        if (!enemiesInArea.Contains(enemy))
        {
            enemiesInArea.Add(enemy);
            attackTimer.Add(0);
        }
    }

    public void _on_seekArea_body_entered(Node body)
    {
        if (body is not Character character || body == this) return;
        
        //если нпц видит игрока, и он дружественный
        if (character is Player && npc.relation == Relation.Friend && !npc.aggressiveAgainstPlayer) return;

        //если нпц видит нпц, и они в одной "фракции"
        if (character is NPC localNpc && localNpc.relation == npc.relation)
        {
            alliesInArea.Add(localNpc);
            return;
        }

        //если нпц вообще не должен ни с кем сражаться
        if (npc.relation == Relation.Neitral
            || character is NPC { relation: Relation.Neitral })
        {
            return;
        }

        AddEnemyInArea(character);
    }

    public void _on_seekArea_body_exited(Node body)
    {
        if (body is Character character && body != this)
        {
            //если враг ушел из зоны видимости
            if (npc.GetState() == SetStateEnum.Attack)
            {
                if (character == npc.tempVictim)
                {
                    if (CheckHiding())
                    {
                        return;
                    }

                    npc.SetState(SetStateEnum.Search);
                }
            }

            if (enemiesInArea.Contains(character))
            {
                int i = enemiesInArea.IndexOf(character);
                enemiesInArea.RemoveAt(i);
                attackTimer.RemoveAt(i);
                tempEnemy = 0;
            }

            if (alliesInArea.Contains(character as NPC))
            {
                alliesInArea.Remove(character as NPC);
            }
        }
    }

    private float GetSeeTimerSpeed(Character victim)
    {
        float speed = INCREASE_TIMER;
        if (victim is Player player)
        {
            if (player.IsCrouching) speed *= CROUCH_MULTIPLY;
            if (player.Velocity.Length() > 7f) speed *= WALK_MULTIPLY;
            if (player.GetNode<LightsCheck>("lightsCheck").OnLight) speed *= LIGHT_MULTIPLY;
            if (player.StealthBoy != null) speed *= STEALTH_BUCK_MULTIPLY;
        }
        else
        {
            speed *= NPC_MULTIPLY;
        }

        return speed;
    }

    private bool CheckHiding()
    {
        return npc.Covers is { IsHidingInCover: true } && !string.IsNullOrEmpty(npc.Weapons.WeaponCode);
    }

    private bool SeeCharacter(Character character)
    {
        switch (character)
        {
            case null:
            case Player { IsInvisibleForEnemy: true }:
                return false;
        }

        var charPos = character.GlobalTransform.origin;
        charPos.y += 0.5f;
        var dir = charPos - ray.GlobalTransform.origin;
        ray.CastTo = dir;
        return ray.GetCollider() == character;
    }
}