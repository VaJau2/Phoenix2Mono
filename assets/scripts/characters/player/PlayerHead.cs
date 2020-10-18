using Godot;
using System;

public class PlayerHead : MeshInstance
{
    const float SHY_TIMER = 10f;
    private static string path = "res://assets/materials/player/";
    private Material openEyes = GD.Load<Material>(path + "player_body.material");
    private Material closeEyes = GD.Load<Material>(path + "player_body_closed_eyes.material");

    private Material openEyesSmiling = GD.Load<Material>(path + "player_body_smiling.material");
    private Material closeEyesSmiling = GD.Load<Material>(path + "player_body_smiling_closed_eyes.material");

    private Material openEyesShy = GD.Load<Material>(path + "player_body_shy.material");
    private Material closedEyesShy = GD.Load<Material>(path + "player_body_shy_closed_eyes.material");

    private bool smiling = false;
    private bool eyesClosed = false;
    private float closedTimer = 5f;
    private float shyTimer = 0;

    Random rand = new Random();

    void OpenEyes() {
         if(shyTimer > 0) {
            SetSurfaceMaterial(0, openEyesShy);
        } else if (smiling) {
            SetSurfaceMaterial(0, openEyesSmiling);
        } else {
            SetSurfaceMaterial(0, openEyes);
        }
        closedTimer = (float)rand.Next(3, 6);
    }

    void CloseEyes() {
        if(shyTimer > 0) {
            SetSurfaceMaterial(0, closedEyesShy);
            closedTimer = 0.15f;
        } else if (smiling) {
            SetSurfaceMaterial(0, closeEyesSmiling);
            closedTimer = 0.5f;
        } else {
            SetSurfaceMaterial(0, closeEyes);
            closedTimer = 0.3f;
        }
    }

    public void SmileOn() {
        if(shyTimer <= 0 && !smiling) {
            if(eyesClosed) {
                SetSurfaceMaterial(0, closeEyesSmiling);
            } else {
                SetSurfaceMaterial(0, openEyesSmiling);
            }
            smiling = true;
        }
    }

    public void SmileOff() {
        if(shyTimer <= 0 && smiling) {
            if(eyesClosed) {
                SetSurfaceMaterial(0, closeEyes);
            } else {
                SetSurfaceMaterial(0, openEyes);
            }
            smiling = false;
        }
    }

    public void ShyOn() {
        if (shyTimer <= 0) {
            SetSurfaceMaterial(0, openEyesShy);
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
