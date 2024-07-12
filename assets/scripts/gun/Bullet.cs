using Godot;

public class Bullet : Area
{
    [Export] public float speed;
    public int Damage;
    public float Timer;
    public Character Shooter;
    
    private void HandleVictim(Spatial victim, int shapeID = 0)
    {
        switch (victim)
        {
            case Character character:
            {
                character.CheckShotgunShot(false);
                Shooter.MakeDamage(character, shapeID);
                if (Shooter is Player)
                {
                    Global.Get().player.Weapons.ShowCrossHitted(shapeID != 0);
                }

                break;
            }
            
            case StaticBody { PhysicsMaterialOverride: null }:
                return;
            
            case StaticBody body:
            {
                if (body is BreakableObject obj)
                {
                    obj.Brake(Damage);
                }

                break;
            }
        }

        return;
    }


    public void _on_plasma_body_shape_entered(int bodyId, Node body, int bodyShape, int areaShape)
    {
        if (body == Shooter)
        {
            return;
        }
        
        HandleVictim(body as Spatial, bodyShape);
        QueueFree();
    }

    public override void _Ready()
    {
    }

    public override void _Process(float delta)
    {
        Translate(Vector3.Forward * speed);

        if (Timer > 0)
        {
            Timer -= delta;
        }
        else
        {
            QueueFree();
        }
    }
}
