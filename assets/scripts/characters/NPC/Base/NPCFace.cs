using Godot;
using System;
using Generic = System.Collections.Generic;
using Godot.Collections;

//класс отвечает за лицо НПЦ
//за текстуры глаз и рта
public class NPCFace : MeshInstance, ISavable
{
    [Export] public string npcName;
    [Export] protected string startEyesVariant = "";
    [Export] protected string startMouthVariant = "A";

    private Generic.Dictionary<string, StreamTexture> mouthTextures =
        new Generic.Dictionary<string, StreamTexture>();

    NPC npc;
    private SpatialMaterial eyesMaterial;
    private SpatialMaterial mouthMaterial;
    private StreamTexture openEyes;
    private StreamTexture closedEyes;
    private bool eyesAreOpen = true;
    private float eyesOpenCooldown = 1f;

    Random rand = new Random();
    Global global => Global.Get();

    public virtual void CloseEyes()
    {
        eyesAreOpen = false;
        eyesMaterial.AlbedoTexture = closedEyes;
        eyesOpenCooldown = 0.2f;
    }

    public void ChangeMouthVariant(string variant)
    {
        StreamTexture mouthTexture =
            GD.Load<StreamTexture>("res://assets/textures/characters/" + npcName + "/mouth/" + variant + ".png");
        mouthMaterial.AlbedoTexture = mouthTexture;
        startMouthVariant = variant;
    }

    public void ChangeEyesVariant(string variantName)
    {
        string path = "res://assets/textures/characters/" + npcName + "/eyes/" + variantName;
        openEyes = GD.Load<StreamTexture>(path + "/0.png");
        closedEyes = GD.Load<StreamTexture>(path + "/1.png");
        eyesMaterial.AlbedoTexture = eyesAreOpen ? openEyes : closedEyes;
        startEyesVariant = variantName;
    }

    private Generic.IEnumerable<AnimTime> LoadTimingFile(string fileName)
    {
        var animation = new Generic.List<AnimTime>();

        string path = "res://assets/audio/dialogue/" + npcName + "/" + fileName;
        var file = new File();
        file.Open(path, File.ModeFlags.Read);
        while (!file.EofReached())
        {
            string line = file.GetLine();
            if (line.Length <= 0) continue;

            string[] parts = line.Split("	");
            float newTime = Global.ParseFloat(parts[0]);
            animation.Add(new AnimTime
            {
                time = newTime,
                name = parts[1]
            });
        }

        file.Close();

        return animation;
    }

    //если появится необходимость анимировать рты для неписей
    //этот метод нужно будет пихнуть в _ready()
    private void LoadMouthTextures()
    {
        string[] mouthVariants = new string[] {"A", "B", "C", "D", "E", "F", "G", "H", "X"};
        foreach (string tempVariant in mouthVariants)
        {
            StreamTexture mouthTexture =
                GD.Load<StreamTexture>("res://assets/textures/characters/" + npcName + "/mouth/" + tempVariant +
                                       ".png");
            mouthTextures.Add(tempVariant, mouthTexture);
        }
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
                eyesMaterial.AlbedoTexture = eyesAreOpen ? openEyes : closedEyes;
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
            eyesMaterial.AlbedoTexture = closedEyes;
            SetProcess(false);
        }
    }

    public override void _Ready()
    {
        npc = GetNode<NPC>("../../../");
        eyesMaterial = (SpatialMaterial) Mesh.SurfaceGetMaterial(1);
        mouthMaterial = (SpatialMaterial) Mesh.SurfaceGetMaterial(2);

        ChangeEyesVariant(startEyesVariant);
    }

    public override void _Process(float delta)
    {
        if (Visible)
        {
            UpdateOpenEyes(delta);
        }
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

public struct AnimTime
{
    public string name;
    public float time;
}