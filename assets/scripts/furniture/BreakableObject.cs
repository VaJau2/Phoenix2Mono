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

    private float itemHeight = 0.4f;
    private List<Character> HearingEnemies = new List<Character>();

    private AudioStreamPlayer3D audi;

    public async void Brake(float damage)
    {
        foreach(Character enemy in HearingEnemies)
        {
            GD.Print(enemy.Name + " heard your noise, but can't react now :c");
            GD.Print("please go to BreakableObject.cs to fix this");
        }

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

            await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
            QueueFree();
        }
    }

    public void _on_enemyArea_body_entered(Spatial body)
    {
        if (body is Character)
        {
            GD.Print("someone walked inside hearing area, but I dont't know what to do :c");
            GD.Print("please go to BreakableObject.cs to fix this");
        }
    }

    public void _on_enemyArea_body_exited(Spatial body)
    {
        if (body is Character)
        {
            Character character = body as Character;
            if (HearingEnemies.Contains(character))
            {
                GD.Print("I guess you know what happened");
                GD.Print("please go to BreakableObject.cs to fix this");
            }
        }
    }

    public override void _Ready()
    {
        audi = GetNode<AudioStreamPlayer3D>("audi");
    }

}

public enum BreakableObjectType {
    Box,
    Lamp,
    Mirror
}