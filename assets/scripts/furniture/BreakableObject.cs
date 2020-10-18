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
    public PackedScene ItemPrefab;
    [Export]
    public Dictionary<string, AudioStreamSample> brakeSounds = new Dictionary<string, AudioStreamSample>
    {
        {"brake1", null},
        {"brake2", null}
    };

    private float itemHeight = 0.4f;
    private List<Character> HearingEnemies = new List<Character>();

    private AudioStreamPlayer audi;

    private async void dropItem() 
    {
        var item = (Spatial)ItemPrefab.Instance();
        GetNode<Spatial>("/root/Main/Scene/items").AddChild(item);
        item.GlobalTransform = 
            Global.setNewOrigin(item.GlobalTransform, GlobalTransform.origin);
        var sideX = GD.Randf() - 0.5f;
        var sideZ = GD.Randf() - 0.5f;
        while(itemHeight > 0 && item != null)
        {
            Vector3 newPos = item.GlobalTransform.origin;
            newPos.x += sideX * 0.1f;
            newPos.y += sideZ * 0.1f;
            newPos.z -= 0.1f;
            item.GlobalTransform = Global.setNewOrigin(
                item.GlobalTransform,
                newPos
            );
            itemHeight -= 0.1f;
            await ToSignal(GetTree(), "idle_frame");
        }
    }

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
                case (BreakableObjectType.Mirror):
                    GD.Print("mirror brakes, but you don't have code to off it :c");
                    GD.Print("please go to BreakableObject.cs to fix this");
                    break;
                
                case (BreakableObjectType.Lamp): 
                    audi.Stream = brakeSounds["brake1"];
                    break;
                
                case (BreakableObjectType.Box):
                    audi.Stream = brakeSounds["brake2"];
                    break;
            }

            if (ItemPrefab != null)
            {
                dropItem();
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
        audi = GetNode<AudioStreamPlayer>("audi");
        //TODO
        //добавить сюда добавление объекта в nuclear-скрипты
    }

}

public enum BreakableObjectType {
    Box,
    Lamp,
    Mirror
}