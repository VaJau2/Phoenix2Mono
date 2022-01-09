using Godot;
using System.Collections.Generic;

public class BreakableObject: StaticBody 
{
    public bool Broken;

    [Export]
    public float BrakeDamage = 50;
    [Export]
    public SpatialMaterial BrokenMaterial;
    [Export]
    public BreakableObjectType objectType;
    [Export]
    public Dictionary<string, AudioStreamSample> brakeSounds = new Dictionary<string, AudioStreamSample>
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
                var mesh = GetNode<MeshInstance>("mesh");
                mesh.SetSurfaceMaterial(0, BrokenMaterial);
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

            GetNode<Spatial>("mesh").Visible = false;
            GetNode<CollisionShape>("shape").Disabled = true;
            GetNode<Particles>("Particles").Emitting = true;
            audi.Play();
            isDeleting = true;
        }
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
    }

    public override void _Process(float delta)
    {
        if (!isDeleting) return;

        if (deleteTimer > 0)
        {
            deleteTimer -= delta;
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