using Godot;
using Godot.Collections;

public class SeekArea : Area
{
    const float ATTACK_TIME = 3f;

    const float INCREASE_TIMER = 0.3f;
    const float CROUCH_MULTIPLY = 0.5f;
    const float WALK_MULTIPLY = 4f;
    const float LIGHT_MULTIPLY = 4f;
    const float NPC_MULTIPLY = 2f;

    Array<Character> enemiesInArea = new Array<Character>();
    Array<float> attackTimer = new Array<float>();
    int tempEnemy = 0;

    NPC npc;
    RayCast ray;

    public override void _Ready()
    {
        npc = GetParent<NPC>();
        ray = GetNode<RayCast>("ray");
    }

    public override void _Process(float delta)
    {
        if (ray.Enabled) {
            //при поворотах неписей луч может идти не в ту точку, поэтому его поворот нужно сначала сбросить
            Vector3 newRayDegrees = ray.RotationDegrees;
            newRayDegrees.y = -npc.RotationDegrees.y;
            ray.RotationDegrees = newRayDegrees;
        }

        if (npc.state == NPCState.Attack) {
            //теряем жертву, если не видим её
            var dir = npc.tempVictim.GlobalTransform.origin - ray.GlobalTransform.origin;
            ray.CastTo = dir;
            ray.Enabled = true;
            if (ray.GetCollider() != npc.tempVictim) {
                npc.SetState(NPCState.Search);
            }
        } else {
            if (enemiesInArea.Count == 0) {
                ray.Enabled = false;
                return;
            }
            //если рядом есть враги
            if (tempEnemy < enemiesInArea.Count) {
                //проверяем их видимость
                var tempVictim = enemiesInArea[tempEnemy];
                var dir = tempVictim.GlobalTransform.origin - ray.GlobalTransform.origin;

                ray.CastTo = dir;
                ray.Enabled = true;
                if (ray.GetCollider() == tempVictim) {
                    //считаем таймер видимости
                    attackTimer[tempEnemy] += GetSeeTimerSpeed(tempVictim);
                    if (attackTimer[tempEnemy] >= ATTACK_TIME) {
                        //идем в атаку на врага, которого заметили
                        npc.tempVictim = tempVictim;
                        npc.SetState(NPCState.Attack);
                    }
                }
            }
        }     
    }

    public void _on_seekArea_body_entered(Node body)
    {
        if (body is Character && body != this) {
            //если нпц видит игрока, и он дружественный
            if (body is Player && npc.relation == Relation.Friend && !npc.aggressiveAgainstPlayer) return;

            //если нпц видит нпц, и они в одной "фракции"
            if (body is NPC && (body as NPC).relation == npc.relation) return;

            
            enemiesInArea.Add(body as Character);
            attackTimer.Add(0);
        }
    }

    public void _on_seekArea_body_exited(Node body)
    {
        if (body is Character && body != this) {
            Character character = body as Character;
            //если враг ушел из зоны видимости
            if (npc.state == NPCState.Attack) {
                if (character == npc.tempVictim) {
                    npc.SetState(NPCState.Search);
                }
            }

            if (enemiesInArea.Contains(character)) {
                int i = enemiesInArea.IndexOf(character);
                enemiesInArea.RemoveAt(i);
                attackTimer.RemoveAt(i);
                tempEnemy = 0;
            }
        }
    }

    private float GetSeeTimerSpeed(Character victim) 
    {
        float speed = INCREASE_TIMER;
        if (victim is Player) {
            var player = victim as Player;
            if (player.IsCrouching) speed *= CROUCH_MULTIPLY;
            if (player.Velocity.Length() > 7f) speed *= WALK_MULTIPLY;
            if (player.GetNode<LightsCheck>("lightsCheck").OnLight) speed *= LIGHT_MULTIPLY;
        } else {
            speed *= NPC_MULTIPLY;
        }
        return speed;
    }
}
