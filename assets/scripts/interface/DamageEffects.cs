using Godot;
using System;

public partial class DamageEffects : TextureRect
{
    const float RED_SPEED = 0.1f;
    private Node3D camera;
    private Vector3 dir;
    private bool moveUp;
    Random rand = new Random();
    
    private Vector3 GetRotateSide(Vector2 angles)
    {
        float angleFront = Mathf.RadToDeg(angles.X);
            //< 130 - игрок смотрит на врага
            //> 55 - игрок смотрит не на врага
        float angleSide = angles.Y;
            //от 0 до 0.5 - налево
            //от -0.5 до 0 - направо
        
        Vector3 side = Vector3.Zero;
        if (angleFront > 130) {
            side.X = 1;
        } else if(angleFront < 55) {
            side.X = -1;
        }

        if (angleSide > -0.5f && angleSide < 0) {
            side.Z = 1;
        } else if(angleSide < 0.5f && angleSide > 0) {
            side.Z = -1;
        }

        return side;
    }

    public void StartEffect() 
    {
        camera = Global.Get().player.Camera3D;
        float randX = rand.Next(0, 180);
        float randY = rand.Next(0, 180);
        Vector2 angles = new Vector2(randX, randY);
        dir = GetRotateSide(angles).Normalized();
        moveUp = true;
        SetProcess(true);
    }

    public override void _Ready()
    {
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (dir == Vector3.Zero) return;
        
        if (moveUp)
        {
            if (Modulate.A < 1)
            {
                camera.Rotate(dir, 0.02f);
                Color modulate = Modulate;
                modulate.A += RED_SPEED * 2f;
                Modulate = modulate;
            }
            else
            {
                moveUp = false;
            }
        }
        else
        {
            if (Modulate.A > 0)
            {
                camera.Rotate(-dir, 0.01f);
                Color modulate = Modulate;
                modulate.A -= RED_SPEED;
                Modulate = modulate;
            }
            else
            {
                SetProcess(false);
            }
        }
        
    }
}
