using Godot;

//класс отвечает за лицо НПЦ: за текстуры глаз и рта
//отличается от NPCFace тем, что меняет не текстуры материала, а меняет сами материалы
public class NpcFaceMat : NpcFace
{
    private const int EYES_MATERIAL = 0;
    
    private SpatialMaterial openEyes;
    private SpatialMaterial closedEyes;

    public override void CloseEyes()
    {
        AreEyesOpen = false;
        Mesh.SurfaceSetMaterial(EYES_MATERIAL, closedEyes);
        EyesOpenCooldown = 0.2f;
    }

    private new void ChangeEyesVariant(string variantName)
    {
        var path = "res://assets/materials/characters/" + npcName + "/eyes/" + variantName;
        openEyes = GD.Load<SpatialMaterial>(path + "/0.material");
        closedEyes = GD.Load<SpatialMaterial>(path + "/1.material");
        Mesh.SurfaceSetMaterial(EYES_MATERIAL, AreEyesOpen ? openEyes : closedEyes);
    }

    protected override void UpdateOpenEyes(float delta)
    {
        if (EyesOpenCooldown > 0)
        {
            EyesOpenCooldown -= delta;
        }
        else
        {
            AreEyesOpen = !AreEyesOpen;
            Mesh.SurfaceSetMaterial(EYES_MATERIAL, AreEyesOpen ? openEyes : closedEyes);
            if (AreEyesOpen)
            {
                EyesOpenCooldown = Rand.Next(3, 6);
            }
            else
            {
                EyesOpenCooldown = 0.2f;
            }
        }
    }

    protected override void DeadFace()
    {
        var deadMesh = (Mesh)Mesh.Duplicate();
        deadMesh.SurfaceSetMaterial(EYES_MATERIAL, closedEyes);
        Mesh = deadMesh;
        
        AreEyesOpen = false;
        SetProcess(false);
    }

    public override void _Ready()
    {
        Npc = GetNode<NPC>("../../../");
        ChangeEyesVariant(startEyesVariant);
    }
}
