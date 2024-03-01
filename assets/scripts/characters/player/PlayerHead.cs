using Godot;
using Godot.Collections;
using System;

public partial class PlayerHead : MeshInstance3D
{
    const float SHY_TIMER = 10f;
    private static string path = "res://assets/textures/characters/player/emotions/";
    private Dictionary<string, CompressedTexture2D> openEyes = new();
    private Dictionary<string, CompressedTexture2D> closeEyes = new();
    private StandardMaterial3D bodyMaterial;
    private Player player => Global.Get().player;

    private string emotion = "empty";
    private bool eyesClosed = false;
    private float closedTimer = 5f;
    private float shyTimer = 0;

    Random rand = new Random();

    public void FindFaceMaterial()
    {
        int count = GetSurfaceOverrideMaterialCount();
        for (int i = 0; i < count; i++)
        {
            StandardMaterial3D tempMaterial = Mesh.SurfaceGetMaterial(i) as StandardMaterial3D;
            if (tempMaterial.DetailEnabled)
            {
                bodyMaterial = tempMaterial;
                return;
            }
        }
    }

    public void PermanentlyCloseEyes()
    {
        var closedEyesMateralPath = "res://assets/materials/player/player_body_closed_eyes.material";
        var material = GD.Load<StandardMaterial3D>(closedEyesMateralPath);
        SetSurfaceOverrideMaterial(0, material);
    }

    public void CloseEyes()
    {
        ChangeMaterialTexture(false);
        eyesClosed = true;
        closedTimer = 0.2f;
    }

    public void SmileOn()
    {
        if (shyTimer <= 0 && emotion != "smile" && !isOnMeds)
        {
            emotion = "smile";
            ChangeMaterialTexture(!eyesClosed);
        }
    }

    public void SmileOff()
    {
        if (shyTimer <= 0 && emotion == "smile" && !isOnMeds)
        {
            emotion = "empty";
            ChangeMaterialTexture(!eyesClosed);
        }
    }

    public void ShyOn()
    {
        if (shyTimer <= 0 && emotion != "shy" && !isOnMeds)
        {
            emotion = "shy";
            ChangeMaterialTexture(!eyesClosed);
        }

        shyTimer = SHY_TIMER;
    }

    public void MedsOn()
    {
        emotion = "meds";
        ChangeMaterialTexture(!eyesClosed);
    }

    public void MedsAfterOn()
    {
        emotion = "meds_after";
        ChangeMaterialTexture(!eyesClosed);
    }

    public void SetEmptyFace()
    {
        emotion = "empty";
        ChangeMaterialTexture(!eyesClosed);
    }

    private bool isOnMeds
    {
        get => emotion == "meds" || emotion == "meds_after";
    }

    private void ChangeMaterialTexture(bool eyesAreOpen)
    {
        CompressedTexture2D newTexture;
        newTexture = eyesAreOpen ? openEyes[emotion] : closeEyes[emotion];
        bodyMaterial.DetailAlbedo = newTexture;
    }

    private void OpenEyes()
    {
        ChangeMaterialTexture(true);
        closedTimer = (float)rand.Next(3, 6);
        eyesClosed = false;
    }

    private void ShyOff()
    {
        if (shyTimer <= 0 && emotion == "shy")
        {
            SetEmptyFace();
        }
    }

    //после перезагрузки ГГ спавнится с закрытыми глазами
    private async void StartOpenEyes()
    {
        await (ToSignal(GetTree(), "process_frame"));
        emotion = "empty";
        OpenEyes();
    }


    public override void _Ready()
    {
        openEyes.Add("empty", GD.Load<CompressedTexture2D>(path + "player_body.png"));
        closeEyes.Add("empty", GD.Load<CompressedTexture2D>(path + "player_body_closed_eyes.png"));

        openEyes.Add("smile", GD.Load<CompressedTexture2D>(path + "player_body_smiling.png"));
        closeEyes.Add("smile", GD.Load<CompressedTexture2D>(path + "player_body_smiling_closed_eyes.png"));

        openEyes.Add("shy", GD.Load<CompressedTexture2D>(path + "player_body_shy.png"));
        closeEyes.Add("shy", GD.Load<CompressedTexture2D>(path + "player_body_shy_closed_eyes.png"));

        openEyes.Add("meds", GD.Load<CompressedTexture2D>(path + "player_body_meds.png"));
        closeEyes.Add("meds", GD.Load<CompressedTexture2D>(path + "player_body_meds_closed_eyes.png"));

        openEyes.Add("meds_after", GD.Load<CompressedTexture2D>(path + "player_body_meds_after.png"));
        closeEyes.Add("meds_after", GD.Load<CompressedTexture2D>(path + "player_body_meds_after_closed_eyes.png"));

        StartOpenEyes();
    }

    public override void _Process(double delta)
    {
        //тело игрока всегда видимо, но от 1 лица оно только бросает тени
        if (CastShadow == ShadowCastingSetting.On)
        {
            if (player is { Health: > 0 })
            {
                if (shyTimer > 0) shyTimer -= (float)delta;
                else ShyOff();

                if (closedTimer > 0)
                {
                    closedTimer -= (float)delta;
                }
                else
                {
                    if (eyesClosed) OpenEyes();
                    else CloseEyes();
                }
            }
            else
            {
                if (!eyesClosed) CloseEyes();
            }
        }
    }
}