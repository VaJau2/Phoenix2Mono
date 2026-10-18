using System.Collections.Generic;
using Godot;

//толкание трупов (рэгдоллов) игроком
//физические кости живого игрока выключены (слой 0): в Godot 4 они кинематические и толкали
//кости трупов с бесконечной силой, проталкивая их сквозь стены.
//Вместо этого кости трупов, которых касается коллизия игрока, разгоняются до скорости игрока
//ограниченным импульсом — стены и пол их удерживают
public class PlayerRagdollPusher
{
    private const uint DEAD_BONES_MASK = 4; // слой 3 — кости трупов (NpcSkeleton.MakeDead)
    private const float MIN_SPEED = 0.5f;
    private const float PUSH_FACTOR = 0.3f; // доля недостающей скорости, добавляемая за кадр
    private const float SHAPE_MARGIN = 0.15f;

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
        var speed = hvel.Length();
        if (speed < MIN_SPEED) return;

        var space = player.GetWorld3D().DirectSpaceState;
        var moveDir = hvel / speed;
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

                //вперёд по движению и немного в сторону от игрока, чтобы труп «расступался»
                var away = bone.GlobalPosition - player.GlobalPosition;
                away.Y = 0;
                var dir = (moveDir + away.Normalized() * 0.5f).Normalized();

                var missingSpeed = speed - bone.LinearVelocity.Dot(dir);
                if (missingSpeed <= 0) continue;
                bone.ApplyCentralImpulse(dir * missingSpeed * bone.Mass * PUSH_FACTOR);
            }
        }
    }
}
