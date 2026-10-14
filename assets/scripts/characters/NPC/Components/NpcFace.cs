using Godot;
using System;
using Godot.Collections;

//класс отвечает за лицо НПЦ: за текстуры глаз и рта
public partial class NpcFace : MeshInstance3D, ISavable
{
    [Export] public string npcName;
    [Export] protected string startEyesVariant = "";
    [Export] protected string startMouthVariant = "A";

    protected NPC Npc;
    protected bool AreEyesOpen = true;
    protected float EyesOpenCooldown = 1f;
    protected readonly Random Rand = new();
    
    private StandardMaterial3D eyesMaterial;
    private StandardMaterial3D mouthMaterial;
    private CompressedTexture2D openEyes;
    private CompressedTexture2D closedEyes;

    public virtual void CloseEyes()
    {
        AreEyesOpen = false;
        eyesMaterial.AlbedoTexture = closedEyes;
        EyesOpenCooldown = 0.2f;
    }

    public void ChangeMouthVariant(string variant)
    {
        var mouthTexturePath = $"res://assets/textures/characters/{npcName}/mouth/{variant}.png";
        var mouthTexture = GD.Load<CompressedTexture2D>(mouthTexturePath);
        mouthMaterial.AlbedoTexture = mouthTexture;
        startMouthVariant = variant;
    }

    public void ChangeEyesVariant(string variantName)
    {
        var path = $"res://assets/textures/characters/{npcName}/eyes/{variantName}";
        openEyes = GD.Load<CompressedTexture2D>(path + "/0.png");
        closedEyes = GD.Load<CompressedTexture2D>(path + "/1.png");
        eyesMaterial.AlbedoTexture = AreEyesOpen ? openEyes : closedEyes;
        startEyesVariant = variantName;
    }

    protected virtual void UpdateOpenEyes(float delta)
    {
        if (EyesOpenCooldown > 0)
        {
            EyesOpenCooldown -= delta;
        }
        else
        {
            AreEyesOpen = !AreEyesOpen;
            eyesMaterial.AlbedoTexture = AreEyesOpen ? openEyes : closedEyes;
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

    protected virtual void DeadFace()
    {
        var deadMesh = (Mesh)Mesh.Duplicate();
        var deadEyes = (StandardMaterial3D)deadMesh.SurfaceGetMaterial(1).Duplicate();
        var deadMouth = (StandardMaterial3D)deadMesh.SurfaceGetMaterial(2).Duplicate();
            
        deadEyes.AlbedoTexture = closedEyes;
        deadMesh.SurfaceSetMaterial(1, deadEyes);
        deadMesh.SurfaceSetMaterial(2, deadMouth);
        Mesh = deadMesh;
            
        AreEyesOpen = false;
        SetProcess(false);
    }

    public override void _Ready()
    {
        AddToGroup("savable");
        Npc = GetNode<NPC>("../../../");
        eyesMaterial = (StandardMaterial3D) Mesh.SurfaceGetMaterial(1);
        mouthMaterial = (StandardMaterial3D) Mesh.SurfaceGetMaterial(2);

        ChangeEyesVariant(startEyesVariant);
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        if (Npc.Health > 0)
        {
            UpdateOpenEyes((float)delta);
        }
        else DeadFace();
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"startEyes", startEyesVariant},
            {"startMouth", startMouthVariant},
        };
    }

    public void LoadData(Dictionary data)
    {
        if (startEyesVariant != data["startEyes"].ToString())
        {
            ChangeEyesVariant(data["startEyes"].ToString());
        }

        if (startMouthVariant != data["startMouth"].ToString())
        {
            ChangeMouthVariant(startMouthVariant);
        }
    }
}
