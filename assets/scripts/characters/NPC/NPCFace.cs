using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

//класс отвечает за лицо НПЦ
//за текстуры глаз и рта
public class NPCFace : MeshInstance
{
    [Export]
    public string npcName;
    [Export]
    public string startEyesVariant = "";

    private Dictionary<string, StreamTexture> mouthTextures = new Dictionary<string, StreamTexture>();
    
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

    //загружаем файл с анимациями рта и тут же его анимируем
    public async void AnimateMouth(string fileName)
    {
        List<AnimTime> animation = LoadTimingFile(fileName + "_mouth.txt");
        float lastTime = 0;

        foreach(AnimTime animTime in animation) {
            mouthMaterial.AlbedoTexture = mouthTextures[animTime.name];
            await global.ToTimer(animTime.time - lastTime);
            lastTime = animTime.time;
        }
    }

    private List<AnimTime> LoadTimingFile(string fileName)
    {
        List<AnimTime> animation = new List<AnimTime>();

        string path = "res://assets/audio/dialogue/" + npcName + "/" + fileName;
        var file = new File();
        file.Open(path, File.ModeFlags.Read);
        while (!file.EofReached()) {
            string line = file.GetLine();
            if (line.Length <= 0) continue;

            string[] parts = line.Split("	");
            float newTime = float.Parse(parts[0], CultureInfo.InvariantCulture);
            animation.Add(new AnimTime {
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
        foreach(string tempVariant in mouthVariants) {
            StreamTexture mouthTexture = GD.Load<StreamTexture>("res://assets/textures/characters/" + npcName + "/mouth/" + tempVariant + ".png");
            mouthTextures.Add(tempVariant, mouthTexture);
        }
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