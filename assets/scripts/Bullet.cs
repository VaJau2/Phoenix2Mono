using Godot;

public class Bullet : Area
{
    [Export]
    public float speed;
    public int Damage;
    public Character Shooter;

    PackedScene gunParticlesPrefab;

    private string handleVictim(Spatial victim, int shapeID = 0)
    {
        string name = null;
        if(victim is Character) {
            if (victim.Name.Contains("target") ||
                victim.Name.Contains("roboEye") ||
                victim.Name.Contains("MrHandy")) {
                    name = "black";
                } else {
                    name = "blood";
                }
            var character = victim as Character;
            character.CheckShotgunShot(false);
            Shooter.MakeDamage(character, shapeID);

        } else if (victim is StaticBody) {
            var body = victim as StaticBody;
            name = MatNames.GetMatName(body.PhysicsMaterialOverride.Friction);
            if (victim is BreakableObject) {
                var obj = victim as BreakableObject;
                obj.Brake(Damage);
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
        gunParticles.GlobalTransform = Global.setNewOrigin(
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
    }
}
