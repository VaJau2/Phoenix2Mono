using Godot;
using Godot.Collections;

public class PlayerDead : KinematicBody, IInteractable, IChest, ISavable
{
    private Race race;
    private string clothCode;
    private string artifactCode;

    private Skeleton skeleton;
    private MeshInstance cloth;
    private MeshInstance artifact;
    private Texture closedEyesTexture;

    public string ChestCode => "body";
    public ChestHandler ChestHandler { get; private set; }

    public bool MayInteract => true;
    public string InteractionHintCode => "search";
    public void Interact(PlayerCamera interactor)
    {
        ChestHandler.Open();
    }
    
    public override void _Ready()
    {
        skeleton = GetNode<Skeleton>("player_body/Armature/Skeleton");
        cloth = skeleton.GetNode<MeshInstance>("Body_third");
        artifact = skeleton.GetNode<MeshInstance>("artifact");
        closedEyesTexture = GD.Load<Texture>("res://assets/textures/characters/player/emotions/player_body_closed_eyes.png");
        CloseEyes();

        ChestHandler = new ChestHandler(this).SetCode(ChestCode);
        ChestHandler.TakeItemEvent += OnTakeItem;
    }

    public override void _ExitTree()
    {
        ChestHandler.TakeItemEvent -= OnTakeItem;
    }

    public void Set(Race newRace, string newClothCode, string newArtifactCode, bool updateMesh = true)
    {
        race = newRace;
        clothCode = newClothCode;
        artifactCode = newArtifactCode;

        if (!updateMesh) return;
        
        ChangeCloth(newClothCode);
        ChangeArtifact(newArtifactCode);
        CloseEyes();
    }

    private void ChangeCloth(string newClothCode)
    {
        var path = $"res://assets/models/player_variants/{newClothCode}/third/{race.ToString().ToLower()}.res";
        cloth.Mesh = GD.Load<Mesh>(path);
        clothCode = newClothCode;
    }

    private void ChangeArtifact(string newArtifactCode)
    {
        artifactCode = newArtifactCode;
        
        if (string.IsNullOrEmpty(newArtifactCode))
        {
            artifact.Visible = false;
            return;
        }
        
        var path = $"res://assets/models/player_variants/artifacts/{artifactCode}/mesh.res";
        artifact.Mesh = GD.Load<Mesh>(path);
        artifact.Visible = true;
    }

    private void OnTakeItem(string takenItem)
    {
        if (takenItem != clothCode && takenItem != artifactCode) return;
        
        var itemData = ItemJSON.GetItemData(takenItem);
        Global.Get().player.Inventory.SoundUsingItem(itemData);

        if (takenItem == clothCode)
        {
            ChangeCloth("empty");
        }
        else
        {
            ChangeArtifact("");
        }
        
        CloseEyes();
    }

    private void CloseEyes()
    {
        var deadMesh = (Mesh)cloth.Mesh.Duplicate();
        var deadMaterial = (SpatialMaterial)deadMesh.SurfaceGetMaterial(0).Duplicate();
        deadMaterial.DetailAlbedo = closedEyesTexture;
        deadMesh.SurfaceSetMaterial(0, deadMaterial);
        cloth.Mesh = deadMesh;
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
        artifactCode = data["artifactCode"].ToString();
        clothCode = data["clothCode"].ToString();
        Set(race, clothCode, artifactCode);
        
        skeleton.PhysicalBonesStartSimulation();
        ChestHandler.LoadData(data);

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
        saveData["artifactCode"] = artifactCode;
        saveData["clothCode"] = clothCode;
        
        return DictionaryHelper.Merge(saveData, ChestHandler.GetSaveData());
    }
}
