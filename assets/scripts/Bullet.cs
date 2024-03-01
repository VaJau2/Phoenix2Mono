using Godot;

public partial class Bullet : Area3D
{
    [Export]
    public float speed;
    public int Damage;
    public double Timer;
    public Character Shooter;

    private PackedScene gunParticlesPrefab;

    private string handleVictim(Node3D victim, int shapeID = 0)
    {
        string name = null;
        switch (victim)
        {
            case Character character:
            {
                var charName = character.Name.ToString();
                if (charName.Contains("target") ||
                    charName.Contains("roboEye") ||
                    charName.Contains("MrHandy")) 
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
            case StaticBody3D body when body.PhysicsMaterialOverride == null:
                return null;
            case StaticBody3D body:
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

        var gunParticles = gunParticlesPrefab.Instantiate<Node3D>();
        GetNode("/root/Main/Scene").AddChild(gunParticles);
        gunParticles.GlobalTransform = Global.SetNewOrigin(
            gunParticles.GlobalTransform,
            GlobalTransform.Origin
        );
        var matName = handleVictim(body as Node3D, bodyShape);
        gunParticles.Call(
            "_startEmitting", 
            GlobalTransform.Basis.Z, 
            matName,
            "box" //чтобы не спавнилась дырка от пуль, т.к. хз как здесь определить нормаль поверхности
        );
        QueueFree();
    }

    public override void _Ready()
    {
        gunParticlesPrefab = GD.Load<PackedScene>("res://objects/guns/gunParticles.tscn");
    }

    public override void _Process(double delta)
    {
        Translate(Vector3.Forward * speed);

        if (Timer > 0) {
            Timer -= delta;
        } else {
            QueueFree();
        }
    }
}
