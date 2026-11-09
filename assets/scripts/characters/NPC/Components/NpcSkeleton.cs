using Godot;
using Godot.Collections;
using System.Collections.Generic;

public partial class NpcSkeleton : Skeleton3D, ISavable
{
    [Export] private float RagdollImpulse = 500;

    //слой 7 «Corpse»: кости трупов; в маске только у игрока и снарядов
    public const uint CORPSE_LAYER = 64;
    private const float MAX_CORPSE_SPEED = 40f; //быстрее кости трупа не летают, м/с (импульс смерти — до ~35)
    [Export] private NodePath headBonePath, bodyBonePath;

    private readonly List<PhysicalBone3D> bones = [];
    private NPC npc;
    private PhysicalBone3D headBone;
    private PhysicalBone3D bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика
    
    public override void _Ready()
    {
        npc = GetNode<NPC>("../../");
        
        headBone = headBonePath != null ? GetNodeOrNull<PhysicalBone3D>(headBonePath) 
            : GetNodeOrNull<PhysicalBone3D>("Physical Bone neck");
        bodyBone = bodyBonePath != null ? GetNodeOrNull<PhysicalBone3D>(bodyBonePath) 
            : GetNodeOrNull<PhysicalBone3D>("Physical Bone back_2");

        //кости живого NPC кинематические и следуют за анимацией: для физики это тела с бесконечной массой,
        //пробегающий NPC разгонял кости трупа до предела Jolt (500 м/с), и рэгдолл получал NaN (B-31).
        //Слой и маска задаются в MakeDead, как и у игрока (B-17)
        foreach (var boneObject in GetChildren())
        {
            if (boneObject is not PhysicalBone3D bone) continue;
            bone.CollisionLayer = 0;
            bone.CollisionMask = 0;
            bones.Add(bone);
        }

        SetPhysicsProcess(false);
    }

    //ограничение скорости костей трупа: разогнавшийся рэгдолл разлетается по уровню и получает NaN (B-31)
    public override void _PhysicsProcess(double delta)
    {
        foreach (var bone in bones)
        {
            var velocity = bone.LinearVelocity;
            if (velocity.LengthSquared() > MAX_CORPSE_SPEED * MAX_CORPSE_SPEED)
            {
                bone.LinearVelocity = velocity.LimitLength(MAX_CORPSE_SPEED);
            }
        }
    }

    public void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
    }

    // настройки рэгдолла
    // вызывается во время загрузки трупика
    public void MakeDead()
    {
        PhysicalBonesStartSimulation();
            
        foreach (var bone in bones)
        {
            //слой 3 (попадания выстрелов, другие трупы) + слой трупов (7): с ним сталкивается только игрок (B-17).
            //Слоя 2 нет: предметы (маска 3) не должны задевать трупы, иначе NPC вдавливает их в рэгдолл (B-31)
            bone.CollisionLayer = 4 | CORPSE_LAYER;
            bone.CollisionMask = 6; // слои 2 (пол) и 3
        }

        SetPhysicsProcess(true);
    }

    // даёт рэгдоллу импульс
    public void AnimateDeath(Character killer, int shapeID)
    {
        Vector3 dir = GlobalPosition.DirectionTo(killer.GlobalPosition);
        float force = tempShotgunShot ? RagdollImpulse * 1.5f : RagdollImpulse;

        if (shapeID == 0)
        {
            bodyBone?.ApplyCentralImpulse(-dir * force);
        }
        else
        {
            headBone?.ApplyCentralImpulse(-dir * force);
        }
    }
    
    public void LoadData(Dictionary data)
    {
        if (npc.Health > 0) return;
        
        foreach (Node3D bone in GetChildren())
        {
            if (bone is not PhysicalBone3D) continue;

            var newPos = data[$"rb_{bone.Name}_pos"].ToString().ParseToVector3();
            var newRot = data[$"rb_{bone.Name}_rot"].ToString().ParseToVector3();
            var oldScale = bone.Scale;

            var newBasis = Basis.FromEuler(newRot);
            var newTransform = new Transform3D(newBasis, newPos);
            bone.GlobalTransform = newTransform;
            bone.Scale = oldScale;
        }
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        if (npc.Health > 0) return saveData;
        
        foreach (Node3D bone in GetChildren())
        {
            if (bone is not PhysicalBone3D) continue;
            saveData[$"rb_{bone.Name}_pos"] = bone.GlobalTransform.Origin;
            saveData[$"rb_{bone.Name}_rot"] = bone.GlobalTransform.Basis.GetEuler();
        }

        return saveData;
    }
}
