using Godot;
using System;

//класс отвечает за лицо НПЦ
//за текстуры глаз
//отличается от NPCFace тем, что не меняет текстуры материалу
//а меняет сами материалы
public class NPCFaceMat : NPCFace
{
    const int EYES_MATERIAL = 0;

    NPC npc;
    private SpatialMaterial openEyes;
    private SpatialMaterial closedEyes;
    private bool eyesAreOpen = true;
    private float eyesOpenCooldown = 1f;

    Random rand = new Random();

    public override void CloseEyes()
    {
        eyesAreOpen = false;
        Mesh.SurfaceSetMaterial(EYES_MATERIAL, closedEyes);
        eyesOpenCooldown = 0.2f;
    }

    private new void ChangeEyesVariant(string variantName)
    {
        string path = "res://assets/materials/characters/" + npcName + "/eyes/" + variantName;
        openEyes = GD.Load<SpatialMaterial>(path + "/0.material");
        closedEyes = GD.Load<SpatialMaterial>(path + "/1.material");
        Mesh.SurfaceSetMaterial(EYES_MATERIAL, eyesAreOpen ? openEyes : closedEyes);
    }

    private void UpdateOpenEyes(float delta)
    {
        if (npc.Health > 0)
        {
            if (eyesOpenCooldown > 0)
            {
                eyesOpenCooldown -= delta;
            }
            else
            {
                eyesAreOpen = !eyesAreOpen;
                Mesh.SurfaceSetMaterial(EYES_MATERIAL, eyesAreOpen ? openEyes : closedEyes);
                if (eyesAreOpen)
                {
                    eyesOpenCooldown = (float) rand.Next(3, 6);
                }
                else
                {
                    eyesOpenCooldown = 0.2f;
                }
            }
        }
        else
        {
            eyesAreOpen = false;
            Mesh.SurfaceSetMaterial(EYES_MATERIAL, closedEyes);
            SetProcess(false);
        }
    }

    public override void _Ready()
    {
        npc = GetNode<NPC>("../../../");

        ChangeEyesVariant(startEyesVariant);
    }

    public override void _Process(float delta)
    {
        UpdateOpenEyes(delta);
    }
}