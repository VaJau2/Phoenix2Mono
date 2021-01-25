using Godot;
using System;

public class PlayerHead : MeshInstance
{
    const float SHY_TIMER = 10f;
    private static string path = "res://assets/textures/player/emotions/";
    private StreamTexture openEyes = GD.Load<StreamTexture>(path + "player_body.png");
    private StreamTexture closeEyes = GD.Load<StreamTexture>(path + "player_body_closed_eyes.png");

    private StreamTexture openEyesSmiling = GD.Load<StreamTexture>(path + "player_body_smiling.png");
    private StreamTexture closeEyesSmiling = GD.Load<StreamTexture>(path + "player_body_smiling_closed_eyes.png");

    private StreamTexture openEyesShy = GD.Load<StreamTexture>(path + "player_body_shy.png");
    private StreamTexture closedEyesShy = GD.Load<StreamTexture>(path + "player_body_shy_closed_eyes.png");

    private SpatialMaterial bodyMaterial;

    private bool smiling = false;
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

    void ChangeMaterialTexture(StreamTexture texture)
    {
        bodyMaterial.DetailAlbedo = texture;
    }


    void OpenEyes() {
        if(shyTimer > 0) {
            ChangeMaterialTexture(openEyesShy);
        } else if (smiling) {
            ChangeMaterialTexture(openEyesSmiling);
        } else {
            ChangeMaterialTexture(openEyes);
        }
        closedTimer = (float)rand.Next(3, 6);
    }

    public void CloseEyes() {
        if(shyTimer > 0) {
            ChangeMaterialTexture(closedEyesShy);
            closedTimer = 0.15f;
        } else if (smiling) {
            ChangeMaterialTexture(closeEyesSmiling);
            closedTimer = 0.5f;
        } else {
            ChangeMaterialTexture(closeEyes);
            closedTimer = 0.3f;
        }
    }

    public void SmileOn() {
        if(shyTimer <= 0 && !smiling) {
            if(eyesClosed) {
                ChangeMaterialTexture(closeEyesSmiling);
            } else {
                ChangeMaterialTexture(openEyesSmiling);
            }
            smiling = true;
        }
    }

    public void SmileOff() {
        if(shyTimer <= 0 && smiling) {
            if(eyesClosed) {
                ChangeMaterialTexture(closeEyes);
            } else {
                ChangeMaterialTexture(openEyes);
            }
            smiling = false;
        }
    }

    public void ShyOn() {
        if (shyTimer <= 0) {
            ChangeMaterialTexture(openEyesShy);
        }
        shyTimer = SHY_TIMER;
    }

    public override void _Process(float delta)
    {
        if (Visible) {
            if (shyTimer > 0) {
                shyTimer -= delta;
            }

            if (closedTimer > 0) {
                closedTimer -= delta;
            } else {
                eyesClosed = !eyesClosed;
                if (!eyesClosed) {
                    OpenEyes();
                } else {
                    CloseEyes();
                }
            }
        }
    }
}
