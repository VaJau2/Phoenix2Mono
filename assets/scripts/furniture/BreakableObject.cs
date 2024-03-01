using Godot;
using Godot.Collections;

public partial class BreakableObject: StaticBody3D 
{
    public bool Broken;

    [Export]
    public float BrakeDamage = 50;
    [Export]
    public StandardMaterial3D BrokenMaterial;
    [Export]
    public BreakableObjectType objectType;
    [Export]
    public Dictionary<string, AudioStreamWav> brakeSounds = new()
    {
        {"brake1", null},
        {"brake2", null}
    };

    private bool isDeleting;
    private float deleteTimer = 1.5f;

    private AudioStreamPlayer3D audi;

    public void Brake(float damage)
    {
        if (!Broken && damage < BrakeDamage)
        {
            if (BrokenMaterial != null)
            {
                var mesh = GetNode<MeshInstance3D>("mesh");
                mesh.SetSurfaceOverrideMaterial(0, BrokenMaterial);
            }
            Broken = true;
            audi.Stream = brakeSounds["brake1"];
            audi.Play();
        } 
        else 
        {
            switch (objectType)
            {            
                case (BreakableObjectType.Lamp): 
                    audi.Stream = brakeSounds["brake1"];
                    break;
                
                case (BreakableObjectType.Box):
                    audi.Stream = brakeSounds["brake2"];
                    break;
            }

            GetNode<Node3D>("mesh").Visible = false;
            GetNode<CollisionShape3D>("shape").Disabled = true;
            GetNode<GpuParticles3D>("Particles").Emitting = true;
            audi.Play();
            isDeleting = true;
        }
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
    }

    public override void _Process(double delta)
    {
        if (!isDeleting) return;

        if (deleteTimer > 0)
        {
            deleteTimer -= (float)delta;
        }
        else
        {
            Global.AddDeletedObject(Name);
            QueueFree();
        }
    }
}

public enum BreakableObjectType {
    Box,
    Lamp,
    Mirror
}