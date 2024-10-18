using Godot;
using Godot.Collections;

public class NpcSkeleton : Skeleton, ISavable
{
    [Export] private NodePath headBonePath, bodyBonePath;
    
    private NPC npc;
    private PhysicalBone headBone;
    private PhysicalBone bodyBone;
    private bool tempShotgunShot; //для увеличения импульса при получении урона от дробовика
    
    public override void _Ready()
    {
        npc = GetNode<NPC>("../../");
        
        headBone = headBonePath != null ? GetNodeOrNull<PhysicalBone>(headBonePath) 
            : GetNodeOrNull<PhysicalBone>("Physical Bone neck");
        bodyBone = bodyBonePath != null ? GetNodeOrNull<PhysicalBone>(bodyBonePath) 
            : GetNodeOrNull<PhysicalBone>("Physical Bone back_2");
    }
    
    public void CheckShotgunShot(bool isShotgun)
    {
        tempShotgunShot = isShotgun;
    }

    public void MakeDead()
    {
        PhysicalBonesStartSimulation();
            
        foreach (var boneObject in GetChildren())
        {
            if (boneObject is not PhysicalBone bone) continue;
            bone.CollisionLayer = 6;
            bone.CollisionMask = 6;
        }
    }

    public void AnimateDeath(Character killer, int shapeID)
    {
        Vector3 dir = Translation.DirectionTo(killer.Translation);
        float force = tempShotgunShot ? npc.MovingController.RagdollImpulse * 1.5f : npc.MovingController.RagdollImpulse;

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
        
        foreach (Spatial bone in GetChildren())
        {
            if (bone is not PhysicalBone) continue;

            var newPos = data[$"rb_{bone.Name}_pos"].ToString().ParseToVector3();
            var newRot = data[$"rb_{bone.Name}_rot"].ToString().ParseToVector3();
            var oldScale = bone.Scale;

            var newBasis = new Basis(newRot);
            var newTransform = new Transform(newBasis, newPos);
            bone.GlobalTransform = newTransform;
            bone.Scale = oldScale;
        }
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        if (npc.Health > 0) return saveData;
        
        foreach (Spatial bone in GetChildren())
        {
            if (bone is not PhysicalBone) continue;
            saveData[$"rb_{bone.Name}_pos"] = bone.GlobalTransform.origin;
            saveData[$"rb_{bone.Name}_rot"] = bone.GlobalTransform.basis.GetEuler();
        }

        return saveData;
    }
}
