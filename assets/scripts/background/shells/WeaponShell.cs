using Godot;
using Godot.Collections;
using System;

//класс гильзы
//играет звуки падения
//удаляет все, кроме меша, когда перестает двигаться
//перемещает меш в ShellsManager, чтобы он там сохранялся
public class WeaponShell : RigidBody
{
    const float STATIC_TIME = 0.5f;
    const float MIN_STATIC_SPEED = 0.5f;
    const float FALL_TIME = 10f;
    const float AUDI_COOLDOWN = 2;

    [Export]
    public Array<AudioStream> shellSound;

    private MeshInstance mesh;
    private AudioStreamPlayer3D audi;

    float audiCooldown;
    float fallTimer;
    float timer;

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
        mesh = GetNode<MeshInstance>("mesh");
    }

    public override void _Process(float delta)
    {
        if (audiCooldown > 0)
        {
            audiCooldown -= delta;
        }

        if (LinearVelocity.Length() > MIN_STATIC_SPEED)
        {
            if (fallTimer < FALL_TIME)
            {
                fallTimer += delta;
            }
            else
            {
                QueueFree();
            }

            timer = 0;
            return;
        }
        
        if (timer < STATIC_TIME)
        {
            timer += delta;
        } 
        else
        {
            var shellsManager = GetNodeOrNull<ShellsManager>("/root/Main/Scene/shells");
            if (shellsManager != null)
            {
                Vector3 meshPos = GlobalTransform.origin;
                Vector3 meshRot = RotationDegrees;
                RemoveChild(mesh);
                shellsManager.AddShell(mesh);
                mesh.GlobalTransform = Global.SetNewOrigin(
                    mesh.GlobalTransform,
                    meshPos
                );
                mesh.RotationDegrees = meshRot;
            }

            QueueFree();
        }
    }

    public void OnBodyEntered(Node body)
    {
        if (shellSound.Count == 0) return;
        if (audiCooldown > 0) return;

        if (body is StaticBody collideBody && collideBody.PhysicsMaterialOverride != null)
        {
            var friction = collideBody.PhysicsMaterialOverride.Friction;
            var materialName = MatNames.GetMatName(friction);

            if (materialName == "wood"
             || materialName == "dirt"
             || materialName == "stairs"
             || materialName == "stone"
             || materialName == "metal")
            {
                Random rand = new Random();
                int randI = rand.Next(0, shellSound.Count);
                audi.Stream = shellSound[randI];
                audi.Play();
                audiCooldown = AUDI_COOLDOWN;
            }
        }
    }
}
