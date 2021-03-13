using Godot;
using Godot.Collections;
using System;

public class PlayerHead : MeshInstance
{
    const float SHY_TIMER = 10f;
    private static string path = "res://assets/textures/characters/player/emotions/";
    private Dictionary<string, StreamTexture> openEyes = new Dictionary<string, StreamTexture>();
    private Dictionary<string, StreamTexture> closeEyes = new Dictionary<string, StreamTexture>();
    private SpatialMaterial bodyMaterial;
    private Player player => Global.Get().player;

    private string emotion = "empty";
    private bool eyesClosed = false;
    private float closedTimer = 5f;
    private float shyTimer = 0;

    Random rand = new Random();

    public void FindFaceMaterial()
    {
        int count = GetSurfaceMaterialCount();
        for (int i = 0; i < count; i++) {
            SpatialMaterial tempMaterial = Mesh.SurfaceGetMaterial(i) as SpatialMaterial;
            if (tempMaterial.DetailEnabled) {
                bodyMaterial = tempMaterial;
                return;
            }
        }
    }

    public void CloseEyes() 
    {
        ChangeMaterialTexture(false);
        eyesClosed = true;
        closedTimer = 0.2f;
    }

    public void SmileOn() 
    {
        if(shyTimer <= 0 && emotion != "smile" && !isOnMeds) {
            emotion = "smile";
            ChangeMaterialTexture(!eyesClosed);
        }
    }

    public void SmileOff() 
    {
        if(shyTimer <= 0 && emotion == "smile" && !isOnMeds) {
            emotion = "empty";
            ChangeMaterialTexture(!eyesClosed);
        }
    }

    public void ShyOn() 
    {
        if (shyTimer <= 0 && emotion != "shy" && !isOnMeds) {
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
        StreamTexture newTexture;
        if (eyesAreOpen) {
            newTexture = openEyes[emotion];
        } else {
            newTexture = closeEyes[emotion];
        }
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
        if (shyTimer <= 0 && emotion == "shy") {
            SetEmptyFace();
        }
    }


    public override void _Ready()
    {
        openEyes.Add("empty", GD.Load<StreamTexture>(path + "player_body.png"));
        closeEyes.Add("empty", GD.Load<StreamTexture>(path + "player_body_closed_eyes.png"));

        openEyes.Add("smile", GD.Load<StreamTexture>(path + "player_body_smiling.png"));
        closeEyes.Add("smile", GD.Load<StreamTexture>(path + "player_body_smiling_closed_eyes.png"));

        openEyes.Add("shy", GD.Load<StreamTexture>(path + "player_body_shy.png"));
        closeEyes.Add("shy", GD.Load<StreamTexture>(path + "player_body_shy_closed_eyes.png"));

        openEyes.Add("meds", GD.Load<StreamTexture>(path + "player_body_meds.png"));
        closeEyes.Add("meds", GD.Load<StreamTexture>(path + "player_body_meds_closed_eyes.png"));

        openEyes.Add("meds_after", GD.Load<StreamTexture>(path + "player_body_meds_after.png"));
        closeEyes.Add("meds_after", GD.Load<StreamTexture>(path + "player_body_meds_after_closed_eyes.png"));
    }

    public override void _Process(float delta)
    {
        //тело игрока всегда видимо, но от 1 лица оно только бросает тени
        if (CastShadow == ShadowCastingSetting.On) {
            if (player != null && player.Health > 0) {
                if (shyTimer > 0) {
                    shyTimer -= delta;
                } else {
                    ShyOff();
                }

                if (closedTimer > 0) {
                    closedTimer -= delta;
                } else {
                    if (eyesClosed) {
                        OpenEyes();
                    } else {
                        CloseEyes();
                    }
                }
            } else {
                if (!eyesClosed) {
                    CloseEyes();
                }
            }
        }
    }
}
