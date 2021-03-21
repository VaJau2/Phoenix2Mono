using Godot;
using Godot.Collections;

public class EnemiesManager : Node
{
    Player player => Global.Get().player;
    public Array<NPC> enemies = new Array<NPC>();

    public void PlayerLoudShoot(float distance)
    {
        foreach(NPC temp in enemies) {
            if (IsInstanceValid(temp) && temp.Health > 0 && temp.state != NPCState.Attack) {
                Vector3 playerPos = player.GlobalTransform.origin;
                Vector3 enemyPos = temp.GlobalTransform.origin;
                if (playerPos.DistanceTo(enemyPos) <= distance) {
                    temp.tempVictim = player;
                    temp.SetState(NPCState.Search);
                }
            }
        }
    }

    public override void _Ready()
    {
        foreach(Node temp in GetChildren()) {
            if (temp is NPC) {
                NPC npc = temp as NPC;
                if (npc.relation == Relation.Enemy) {
                    enemies.Add(npc);
                }
            }
        }
    }
    
}
