using System.Collections.Generic;
using Godot;

//толкание трупов (рэгдоллов) игроком (B-17)
//Физические кости живого игрока выключены (слой 0): в Godot 4 они кинематические и толкали кости трупов
//с бесконечной силой, проталкивая их сквозь стены. Игрок сталкивается с трупами (слой 7, NpcSkeleton.CORPSE_LAYER),
//а скелет, которого касается коллизия игрока, разгоняется ограниченным импульсом по направлению движения игрока — но не быстрее MAX_SPEED,
//как бы быстро ни двигался игрок (бег земнопони — 30 м/с): за шаг физики кость сдвигается на несколько
//сантиметров, стены её удерживают, суставы не рвутся
public class PlayerRagdollPusher
{
    private const uint DEAD_BONES_MASK = NpcSkeleton.CORPSE_LAYER;
    private const float MIN_SPEED = 0.5f;
    private const float MAX_SPEED = 2.5f;     // предел скорости, до которой разгоняется труп, м/с
    private const float PUSH_FACTOR = 0.15f;  // доля недостающей скорости, добавляемая за кадр
    private const float SHAPE_MARGIN = 0.1f;

    private readonly Player player;
    private readonly CollisionShape3D[] shapes;
    private readonly HashSet<PhysicalBone3D> pushedBones = [];

    public PlayerRagdollPusher(Player player, params CollisionShape3D[] shapes)
    {
        this.player = player;
        this.shapes = shapes;
    }

    private const float SHOT_SPEED = 3f;          // прибавка скорости кости от выстрела, м/с
    private const float SHOTGUN_SHOT_SPEED = 6f;
    private const float SHOT_MAX_SPEED = 8f;      // быстрее этого кость выстрелами не разгоняется

    //толчок трупа выстрелом: импульс в точку попадания по направлению выстрела (кость ещё и поворачивается),
    //ограничен по скорости — серия выстрелов не разгоняет труп до вылета за уровень
    public static void PushByShot(PhysicalBone3D bone, Vector3 hitPoint, Vector3 direction, bool shotgun)
    {
        direction = direction.Normalized();
        float add = shotgun ? SHOTGUN_SHOT_SPEED : SHOT_SPEED;
        add = Mathf.Min(add, SHOT_MAX_SPEED - bone.LinearVelocity.Dot(direction));
        if (add <= 0) return;
        bone.ApplyImpulse(direction * add * bone.Mass, hitPoint - bone.GlobalPosition);
    }

    public void Push(Vector3 velocity)
    {
        var hvel = new Vector3(velocity.X, 0, velocity.Z);
        float speed = hvel.Length();
        if (speed < MIN_SPEED) return;

        var dir = hvel / speed;
        float targetSpeed = Mathf.Min(speed, MAX_SPEED);
        var space = player.GetWorld3D().DirectSpaceState;
        pushedBones.Clear();

        foreach (var shape in shapes)
        {
            var query = new PhysicsShapeQueryParameters3D
            {
                Shape = shape.Shape,
                Transform = shape.GlobalTransform,
                CollisionMask = DEAD_BONES_MASK,
                Margin = SHAPE_MARGIN,
            };

            foreach (var hit in space.IntersectShape(query, 32))
            {
                if (hit["collider"].AsGodotObject() is not PhysicalBone3D touched) continue;
                //разгоняется весь скелет: игрок упирается в крайние лёгкие кости (ноги), и толчок только их
                //удерживают суставы тяжёлого туловища — труп не двигается, а суставы нагружаются
                foreach (var child in touched.GetParent().GetChildren())
                {
                    if (child is not PhysicalBone3D bone || !pushedBones.Add(bone)) continue;

                    float missingSpeed = targetSpeed - bone.LinearVelocity.Dot(dir);
                    if (missingSpeed <= 0) continue;
                    bone.ApplyCentralImpulse(dir * missingSpeed * bone.Mass * PUSH_FACTOR);
                }
            }
        }
    }
}
