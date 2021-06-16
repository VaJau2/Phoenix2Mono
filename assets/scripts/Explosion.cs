using Godot;
using Godot.Collections;

public class Explosion : Spatial
{
    private Array<Particles> parts = new Array<Particles>();
    private AnimationPlayer anim;
    private AudioStreamPlayer3D audi;

    public override void _Ready()
    {
        anim = GetNode<AnimationPlayer>("anim");
        audi = GetNode<AudioStreamPlayer3D>("audi");
        var partsParent = GetNode("parts");
        foreach (Particles tempPart in partsParent.GetChildren())
        {
            parts.Add(tempPart);
        }
    }

    public async void Explode()
    {
        anim.Play("explode");
        audi.Play();
        GetNode<AnimationPlayer>("../wallparts/anim").Play("explode");
        GetNode<CollisionShape>("../WallShape").Disabled = true;
        foreach (var part in parts)
        {
            part.Emitting = true;
        }

        Global.Get().player.shakingSpeed = 1.25f;
        await Global.Get().ToTimer(0.75f);
        Global.Get().player.shakingSpeed = 0;
    }
}
