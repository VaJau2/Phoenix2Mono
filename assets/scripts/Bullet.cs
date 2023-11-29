using Godot;

public class Bullet : Area
{
    [Export]
    public float speed;
    public int Damage;
    public float Timer;
    public Character Shooter;

    private PackedScene gunParticlesPrefab;

    private string handleVictim(Spatial victim, int shapeID = 0)
    {
        string name = null;
        switch (victim)
        {
            case Character character:
            {
                if (character.Name.Contains("target") ||
                    character.Name.Contains("roboEye") ||
                    character.Name.Contains("MrHandy")) 
                {
                    name = "black";
                } 
                else 
                {
                    name = "blood";
                }

                character.CheckShotgunShot(false);
                Shooter.MakeDamage(character, shapeID);
                if (Shooter is Player) {
                    Global.Get().player.Weapons.ShowCrossHitted(shapeID != 0);
                }

                break;
            }
            case StaticBody body when body.PhysicsMaterialOverride == null:
                return null;
            case StaticBody body:
            {
                name = MatNames.GetMatName(body.PhysicsMaterialOverride.Friction);
                if (body is BreakableObject obj) {
                    obj.Brake(Damage);
                }

                break;
            }
        }

        return name;
    }  


    public void _on_plasma_body_shape_entered(int bodyId, Node body, int bodyShape, int areaShape)
    {
        if (body == Shooter) {
            return;
        }

        var gunParticles = (Spatial)gunParticlesPrefab.Instance();
        GetNode("/root/Main/Scene").AddChild(gunParticles);
        gunParticles.GlobalTransform = Global.SetNewOrigin(
            gunParticles.GlobalTransform,
            GlobalTransform.origin
        );
        var matName = handleVictim(body as Spatial, bodyShape);
        gunParticles.Call(
            "_startEmitting", 
            GlobalTransform.basis.z, 
            matName,
            "box" //чтобы не спавнилась дырка от пуль, т.к. хз как здесь определить нормаль поверхности
        );
        QueueFree();
    }

    public override void _Ready()
    {
        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
    }

    public override void _Process(float delta)
    {
        Translate(Vector3.Forward * speed);

        if (Timer > 0) {
            Timer -= delta;
        } else {
            QueueFree();
        }
    }
}
