using Godot;
using Godot.Collections;

public class PlayerDead : KinematicBody, ISavable
{
    private Race race;
    private string cloth;

    private Skeleton skeleton;
    private MeshInstance body;
    private Texture closedEyesTexture;
    
    public override void _Ready()
    {
        skeleton = GetNode<Skeleton>("player_body/Armature/Skeleton");
        body = skeleton.GetNode<MeshInstance>("Body_third");
        closedEyesTexture = GD.Load<Texture>("res://assets/textures/characters/player/emotions/player_body_closed_eyes.png");
        CloseEyes();
    }

    public void Set(Race newRace, string newCloth, bool updateMesh = true)
    {
        race = newRace;
        cloth = newCloth;

        if (!updateMesh) return;
        
        ChangeCloth(newCloth);
        CloseEyes();
    }

    private void ChangeCloth(string newCloth)
    {
        var path = $"res://assets/models/player_variants/{newCloth}/third/{race.ToString().ToLower()}.res";
        var mesh = GD.Load<Mesh>(path);
        body.Mesh = mesh;
        cloth = newCloth;
    }

    private void CloseEyes()
    {
        var deadMesh = (Mesh)body.Mesh.Duplicate();
        var deadMaterial = (SpatialMaterial)deadMesh.SurfaceGetMaterial(0).Duplicate();
        deadMaterial.DetailAlbedo = closedEyesTexture;
        deadMesh.SurfaceSetMaterial(0, deadMaterial);
        body.Mesh = deadMesh;
    }
    
    public void LoadData(Dictionary data)
    {
        GlobalTranslation = data["pos"].ToString().ParseToVector3();
        GlobalRotation = data["rot"].ToString().ParseToVector3(); 
        
        foreach (Spatial bone in skeleton.GetChildren())
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

        race = Global.RaceFromString(data["race"].ToString());
        cloth = data["cloth"].ToString();
        Set(race, cloth);
        
        skeleton.PhysicalBonesStartSimulation();

        if (race == Race.Pegasus) return;
        
        var wings = GetNode<Node>("player_body/Armature/Skeleton/Wings");
        wings.QueueFree();
    }

    public Dictionary GetSaveData()
    {
        var saveData = new Dictionary();
        saveData["pos"] = GlobalTranslation;
        saveData["rot"] = GlobalRotation;
        
        var skeleton = GetNode<Skeleton>("player_body/Armature/Skeleton");
        foreach (Spatial bone in skeleton.GetChildren())
        {
            if (bone is not PhysicalBone) continue;
            saveData[$"rb_{bone.Name}_pos"] = bone.GlobalTransform.origin;
            saveData[$"rb_{bone.Name}_rot"] = bone.GlobalTransform.basis.GetEuler();
        }

        saveData["race"] = Global.RaceToString(race);
        saveData["cloth"] = cloth;
        return saveData;
    }
}
