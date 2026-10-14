using Godot;
using Godot.Collections;

public partial class NpcSkeleton : Skeleton3D, ISavable
{
    [Export] private float RagdollImpulse = 500;
    [Export] private NodePath headBonePath, bodyBonePath;
    
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
            
        foreach (var boneObject in GetChildren())
        {
            if (boneObject is not PhysicalBone3D bone) continue;
            bone.CollisionLayer = 6; // слои 2 и 3
            bone.CollisionMask = 6; // слои 2 и 3
        }
    }

    // даёт рэгдоллу импульс
    public void AnimateDeath(Character killer, int shapeID)
    {
        Vector3 dir = GlobalTranslation.DirectionTo(killer.GlobalTranslation);
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

            var newBasis = new Basis(newRot);
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
            saveData[$"rb_{bone.Name}_pos"] = bone.GlobalTransform.origin;
            saveData[$"rb_{bone.Name}_rot"] = bone.GlobalTransform.basis.GetEuler();
        }

        return saveData;
    }
}
