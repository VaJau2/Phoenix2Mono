using Godot;
using System;

public class DamageEffects : TextureRect
{
    const float RED_SPEED = 0.1f;
    private bool isEffecting = false;
    Random rand = new Random();
    
    private Vector3 GetRotateSide(Vector2 angles)
    {
        float angleFront = Mathf.Rad2Deg(angles.x);
            //< 130 - игрок смотрит на врага
            //> 55 - игрок смотрит не на врага
        float angleSide = angles.y;
            //от 0 до 0.5 - налево
            //от -0.5 до 0 - направо
        
        Vector3 side = Vector3.Zero;
        if (angleFront > 130) {
            side.x = -1;
        } else if(angleFront < 55) {
            side.x = 1;
        }

        if (angleSide > -0.5f && angleSide < 0) {
            side.z = 1;
        } else if(angleSide < 0.5f && angleSide > 0) {
            side.z = -1;
        }

        return side;
    }

    public async void StartEffect() 
    {
        Spatial camera = Global.Get().player.Camera;
        float randX = rand.Next(0, 180);
        float randY = rand.Next(0, 180);
        Vector2 angles = new Vector2(randX, randY);

        Vector3 dir = GetRotateSide(angles);
        if (isEffecting) {
            return;
        }
        isEffecting = true;

        while(Modulate.a < 1) {
            camera.Rotate(dir, 0.02f);
            Color modulate = Modulate;
            modulate.a += RED_SPEED * 2f;
            Modulate = modulate;
            await ToSignal(GetTree(), "idle_frame");
        }

        while(Modulate.a > 0) {
            camera.Rotate(-dir, 0.01f);
            Color modulate = Modulate;
            modulate.a -= RED_SPEED;
            Modulate = modulate;
            await ToSignal(GetTree(), "idle_frame");
        }

        isEffecting = false;
    }
}
