using Godot;
using System;

//класс отвечает за лицо НПЦ
//за текстуры глаз и рта
public class NPCFace : MeshInstance
{
    [Export]
    public string npcName;
    [Export]
    public string startEyesVariant = "";
    
    NPC npc;
    private SpatialMaterial eyesMaterial;
    private SpatialMaterial mouthMaterial;
    private StreamTexture openEyes;
    private StreamTexture closedEyes;
    private bool eyesAreOpen = true;
    private float eyesOpenCooldown = 1f;

    Random rand = new Random();
    Global global => Global.Get();

    public void CloseEyes()
    {
        eyesAreOpen = false;
        eyesMaterial.AlbedoTexture = closedEyes;
        eyesOpenCooldown = 0.2f;
    }

    private void ChangeEyesVariant(string variantName)
    {
        string path = "res://assets/textures/characters/" + npcName + "/eyes/" + variantName;
        openEyes = GD.Load<StreamTexture>(path + "/0.png");
        closedEyes = GD.Load<StreamTexture>(path + "/1.png");
        eyesMaterial.AlbedoTexture = eyesAreOpen ? openEyes : closedEyes;
    }

    private void UpdateOpenEyes(float delta)
    {
        if (npc.Health > 0) {
            if (eyesOpenCooldown > 0) {
                eyesOpenCooldown -= delta;
            } else {
                eyesAreOpen = !eyesAreOpen;
                eyesMaterial.AlbedoTexture = eyesAreOpen ? openEyes : closedEyes;
                if (eyesAreOpen) {
                    eyesOpenCooldown = (float)rand.Next(3, 6);
                } else {
                    eyesOpenCooldown = 0.2f;
                }
            }
        } else {
            eyesAreOpen = false;
            eyesMaterial.AlbedoTexture = closedEyes;
            SetProcess(false);
        }
    }

    public override void _Ready()
    {
        npc = GetNode<NPC>("../../../");
        eyesMaterial  = (SpatialMaterial)Mesh.SurfaceGetMaterial(1);
        mouthMaterial = (SpatialMaterial)Mesh.SurfaceGetMaterial(2);

        ChangeEyesVariant(startEyesVariant);
    }

    public override void _Process(float delta)
    {
        UpdateOpenEyes(delta);
    }
}

public struct AnimTime
{
    public string name;
    public float time;
}