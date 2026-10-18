using System.Collections.Generic;
using Godot;

//Jolt (физика Godot 4) не умеет неравномерно масштабировать сферы, капсулы, цилиндры и формы, повёрнутые
//внутри неравномерно масштабированного тела (в т. ч. выпуклые оболочки), — пишет «Failed to correctly scale
//body/shape» и подменяет масштаб равномерным: коллизия меняет форму. В Godot 3 это работало (B-14).
//Неравномерный масштаб Jolt выдерживает только у коробок без поворота и у сеток треугольников.
//
//Исправление (положение коллизии в мире не меняется):
//- тело только из коллизий (триггеры Area3D, невидимые стены): с тела снимается масштаб, он целиком
//  «запекается» в формы — объём сохраняется, триггеры работают как раньше;
//- статичное тело, у проблемной формы которого есть дочерние модели (двери: анимация поворачивает форму
//  вместе с моделью): масштаб тела переносится в трансформы всех дочерних узлов, форма — сеткой треугольников;
//- остальные статичные тела с моделями (мебель, камни, холмы): проблемные формы — сеткой треугольников
//  с компенсирующим масштабом (тело, модели и анимации не трогаются);
//- при равномерном масштабе тела (повёрнутые/масштабированные сферы и цилиндры манекенов) — выпуклой оболочкой.
//Jolt собирает коллизию сразу при добавлении тела в дерево, поэтому уровень исправляется до AddChild
//(FixTree в LevelsLoader); объекты, созданные в игре, — при появлении (NodeAdded), с отключением формы
//на время замены, чтобы не было промежуточных пересборок с ошибкой.
public static class JoltScaleFix
{
    private const float EPSILON = 0.001f;
    private const int MAX_HULL_POINTS = 64;

    public static void Connect(SceneTree tree)
    {
        tree.NodeAdded += node =>
        {
            //после текущего кода: объект могли передвинуть/масштабировать сразу после AddChild
            if (node is Node3D body && body.IsClass("CollisionObject3D"))
            {
                Callable.From(() => FixInTree(body)).CallDeferred();
            }
        };
    }

    //сцена, ещё не добавленная в дерево (глобальные трансформы считаются вручную от parentGlobal)
    public static void FixTree(Node root, Transform3D parentGlobal)
    {
        var global = parentGlobal;
        if (root is Node3D node3D)
        {
            global = node3D.TopLevel ? node3D.Transform : parentGlobal * node3D.Transform;
            //IsClass, а не «is CollisionObject3D»: у узла со скриптом, унаследованным от Node3D
            //(SavePosition, FurnChest, DoorTeleport...), C#-объект имеет тип скрипта
            if (node3D.IsClass("CollisionObject3D"))
            {
                FixBody(node3D, parentGlobal, global, false);
                global = parentGlobal * node3D.Transform;
            }
        }

        foreach (var child in root.GetChildren())
        {
            FixTree(child, global);
        }
    }

    private static void FixInTree(Node3D body)
    {
        if (!GodotObject.IsInstanceValid(body) || !body.IsInsideTree()) return;
        var parentGlobal = body.GetParent() is Node3D parent && !body.TopLevel ? parent.GlobalTransform : Transform3D.Identity;
        FixBody(body, parentGlobal, body.GlobalTransform, true);
    }

    private static void FixBody(Node3D body, Transform3D parentGlobal, Transform3D global, bool inTree)
    {
        var scale = global.Basis.Scale;
        var shapes = new List<CollisionShape3D>();
        bool pureCollision = true;
        bool anyFails = false;
        foreach (var child in body.GetChildren())
        {
            if (child is CollisionShape3D cs && cs.Shape != null)
            {
                shapes.Add(cs);
                anyFails |= Fails(cs, scale);
            }
            else if (child is Node3D)
            {
                pureCollision = false;
            }
        }
        if (!anyFails) return;

        //к форме привязана модель (двери: анимация поворачивает форму вместе с моделью) —
        //трансформ такой формы менять нельзя
        bool shapeWithVisuals = false;
        foreach (var cs in shapes)
        {
            if (Fails(cs, scale) && HasVisuals(cs)) shapeWithVisuals = true;
        }
        bool isStatic = body.IsClass("StaticBody3D");

        foreach (var cs in shapes) SetDisabled(cs, inTree, true);

        if (!IsUniform(scale) && shapeWithVisuals && isStatic)
        {
            //масштаб тела переносится в трансформы всех дочерних узлов (вид не меняется), тело — без масштаба;
            //проблемные формы — сеткой треугольников в собственных осях (её Jolt масштабирует как угодно)
            var newGlobal = new Transform3D(global.Basis.Orthonormalized(), global.Origin);
            var residual = newGlobal.AffineInverse() * global;
            body.Transform = parentGlobal.AffineInverse() * newGlobal;
            foreach (var child in body.GetChildren())
            {
                if (child is Node3D node) node.Transform = residual * node.Transform;
            }
            foreach (var cs in shapes)
            {
                if (cs.Shape is SphereShape3D or CapsuleShape3D or CylinderShape3D or ConvexPolygonShape3D)
                {
                    var baked = Concave(cs.Shape, Basis.Identity);
                    if (baked != null) cs.Shape = baked;
                }
            }
        }
        else if (!IsUniform(scale) && pureCollision)
        {
            //масштаб снимается с тела и запекается во все его формы
            var newGlobal = new Transform3D(global.Basis.Orthonormalized(), global.Origin);
            body.Transform = parentGlobal.AffineInverse() * newGlobal;
            foreach (var cs in shapes)
            {
                var full = newGlobal.AffineInverse() * global * cs.Transform;
                if (IsUniform(full.Basis.Scale, 0.01f))
                {
                    cs.Transform = full;
                    continue;
                }
                Shape3D baked = cs.Shape is ConcavePolygonShape3D ? (Shape3D)Concave(cs.Shape, full.Basis) : Convex(cs.Shape, full.Basis);
                if (baked == null) continue;
                cs.Shape = baked;
                cs.Transform = new Transform3D(Basis.Identity, full.Origin);
            }
        }
        else
        {
            foreach (var cs in shapes)
            {
                if (!Fails(cs, scale)) continue;
                if (HasVisuals(cs))
                {
                    //трансформ не трогаем: при равномерном масштабе тела хватает сетки треугольников
                    if (IsUniform(scale) && isStatic)
                    {
                        var mesh = Concave(cs.Shape, Basis.Identity);
                        if (mesh != null) cs.Shape = mesh;
                    }
                    continue;
                }
                //форма в осях тела без его масштаба: p -> diag(S) * L.basis * p, масштаб тела компенсируется
                var local = cs.Transform;
                var basis = Basis.FromScale(scale) * local.Basis;
                Shape3D baked = null;
                if (IsUniform(scale))
                {
                    baked = cs.Shape is ConcavePolygonShape3D ? (Shape3D)Concave(cs.Shape, basis) : Convex(cs.Shape, basis);
                }
                else if (isStatic)
                {
                    baked = Concave(cs.Shape, basis);
                }
                if (baked == null) continue;
                cs.Shape = baked;
                cs.Transform = new Transform3D(Basis.FromScale(Inverse(scale)), local.Origin);
            }
        }

        foreach (var cs in shapes) SetDisabled(cs, inTree, false);
    }

    private static bool HasVisuals(CollisionShape3D cs)
    {
        foreach (var child in cs.GetChildren())
        {
            if (child is Node3D) return true;
        }
        return false;
    }

    private static bool Fails(CollisionShape3D cs, Vector3 bodyScale)
    {
        var basis = cs.Transform.Basis;
        bool round = cs.Shape is SphereShape3D or CapsuleShape3D or CylinderShape3D;
        //выпуклую оболочку Jolt масштабирует только равномерно (у неё есть радиус скругления)
        bool convex = cs.Shape is ConvexPolygonShape3D;
        bool bodyFails = !IsUniform(bodyScale) && (round || convex || !IsAxisAligned(basis));
        bool shapeFails = !IsUniform(basis.Scale) && (round || convex);
        return bodyFails || shapeFails;
    }

    //в дереве — отключение формы на время замены (без промежуточных пересборок коллизии)
    private static void SetDisabled(CollisionShape3D cs, bool inTree, bool value)
    {
        if (!inTree) return;
        if (value) cs.SetMeta("jolt_fix_was_disabled", cs.Disabled);
        else if (cs.HasMeta("jolt_fix_was_disabled"))
        {
            value = cs.GetMeta("jolt_fix_was_disabled").AsBool();
            cs.RemoveMeta("jolt_fix_was_disabled");
        }
        cs.Disabled = value;
    }

    // --- запекание форм ---------------------------------------------------------------------------

    private static ConvexPolygonShape3D Convex(Shape3D shape, Basis basis)
    {
        var points = new List<Vector3>();
        switch (shape)
        {
            case ConvexPolygonShape3D convex:
                foreach (var p in convex.Points) points.Add(basis * p);
                break;
            case BoxShape3D or SphereShape3D or CapsuleShape3D or CylinderShape3D:
                foreach (var p in PrimitiveFaces(shape)) points.Add(basis * p);
                break;
            default:
                return null;
        }
        return new ConvexPolygonShape3D { Points = points.ToArray() };
    }

    private static ConcavePolygonShape3D Concave(Shape3D shape, Basis basis)
    {
        Vector3[] faces;
        bool backface = false;
        switch (shape)
        {
            case ConcavePolygonShape3D concave:
                faces = concave.GetFaces();
                backface = concave.BackfaceCollision;
                break;
            case ConvexPolygonShape3D convex:
                faces = HullFaces(convex.Points);
                break;
            default:
                faces = PrimitiveFaces(shape);
                break;
        }
        if (faces == null || faces.Length == 0) return null;
        for (int i = 0; i < faces.Length; i++) faces[i] = basis * faces[i];
        var result = new ConcavePolygonShape3D { BackfaceCollision = backface };
        result.SetFaces(faces);
        return result;
    }

    //треугольники примитива той же формы (сферы/капсулы/цилиндры — с умеренным числом сегментов)
    private static Vector3[] PrimitiveFaces(Shape3D shape)
    {
        PrimitiveMesh mesh = shape switch
        {
            BoxShape3D box => new BoxMesh { Size = box.Size },
            SphereShape3D sphere => new SphereMesh { Radius = sphere.Radius, Height = sphere.Radius * 2, RadialSegments = 16, Rings = 8 },
            CapsuleShape3D capsule => new CapsuleMesh { Radius = capsule.Radius, Height = capsule.Height, RadialSegments = 16, Rings = 4 },
            CylinderShape3D cylinder => new CylinderMesh { TopRadius = cylinder.Radius, BottomRadius = cylinder.Radius, Height = cylinder.Height, RadialSegments = 16, Rings = 1 },
            _ => null
        };
        return mesh?.GetFaces();
    }

    //треугольники выпуклой оболочки: перебор троек точек, у которых все точки по одну сторону плоскости
    private static Vector3[] HullFaces(Vector3[] points)
    {
        if (points.Length > MAX_HULL_POINTS) return null;
        var faces = new List<Vector3>();
        var center = Vector3.Zero;
        foreach (var p in points) center += p;
        center /= points.Length;
        float size = 0;
        foreach (var p in points) size = Mathf.Max(size, p.DistanceTo(center));
        float eps = size * 1e-4f;

        for (int i = 0; i < points.Length; i++)
        for (int j = i + 1; j < points.Length; j++)
        for (int k = j + 1; k < points.Length; k++)
        {
            var normal = (points[j] - points[i]).Cross(points[k] - points[i]);
            if (normal.LengthSquared() < eps * eps) continue;
            normal = normal.Normalized();
            float d = normal.Dot(points[i]);
            bool front = false, back = false;
            foreach (var p in points)
            {
                float dist = normal.Dot(p) - d;
                if (dist > eps) front = true;
                else if (dist < -eps) back = true;
                if (front && back) break;
            }
            if (front && back) continue;
            //наружу от центра; Godot: передняя грань — по часовой стрелке
            bool outward = normal.Dot(center) - d < 0;
            if (outward) faces.AddRange(new[] { points[i], points[k], points[j] });
            else faces.AddRange(new[] { points[i], points[j], points[k] });
        }
        return faces.ToArray();
    }

    // --- проверки ------------------------------------------------------------------------------

    private static bool IsUniform(Vector3 s, float epsilon = EPSILON) =>
        Mathf.Abs(s.X - s.Y) <= epsilon * Mathf.Abs(s.X) && Mathf.Abs(s.X - s.Z) <= epsilon * Mathf.Abs(s.X);

    //оси формы совпадают с осями тела (повороты на 90°) — такой поворот Jolt масштабирует корректно
    private static bool IsAxisAligned(Basis basis)
    {
        var b = basis.Orthonormalized();
        foreach (var axis in new[] { b.X, b.Y, b.Z })
        {
            var a = axis.Abs();
            if (Mathf.Max(a.X, Mathf.Max(a.Y, a.Z)) < 1 - 1e-6f) return false;
        }
        return true;
    }

    private static Vector3 Inverse(Vector3 s) => new(1 / s.X, 1 / s.Y, 1 / s.Z);
}
