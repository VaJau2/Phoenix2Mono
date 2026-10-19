using System.Collections.Generic;
using Godot;

//толкание трупов (рэгдоллов) игроком (B-17)
//Физические кости живого игрока выключены (слой 0): в Godot 4 они кинематические и толкали кости трупов
//с бесконечной силой, проталкивая их сквозь стены. Вместо этого кости трупов, которых касается коллизия
//игрока, разгоняются ограниченным импульсом по направлению движения игрока — но не быстрее MAX_SPEED,
//как бы быстро ни двигался игрок (бег земнопони — 30 м/с): за шаг физики кость сдвигается на несколько
//сантиметров, стены её удерживают, суставы не рвутся
public class PlayerRagdollPusher
{
    private const uint DEAD_BONES_MASK = 4; // слой 3 — кости трупов (NpcSkeleton.MakeDead)
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
                if (hit["collider"].AsGodotObject() is not PhysicalBone3D bone) continue;
                if (!pushedBones.Add(bone)) continue;

                float missingSpeed = targetSpeed - bone.LinearVelocity.Dot(dir);
                if (missingSpeed <= 0) continue;
                bone.ApplyCentralImpulse(dir * missingSpeed * bone.Mass * PUSH_FACTOR);
            }
        }
    }
}
