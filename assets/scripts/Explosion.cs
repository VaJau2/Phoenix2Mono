using Godot;
using Godot.Collections;

public class Explosion : Spatial, ISavable
{
    private Array<Particles> parts = new Array<Particles>();
    private AnimationPlayer anim;
    private AudioStreamPlayer3D audi;

    private bool exploded;

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

        exploded = true;

        Global.Get().player.shakingSpeed = 1.25f;
        await Global.Get().ToTimer(0.75f);
        Global.Get().player.shakingSpeed = 0;
    }

    public Dictionary GetSaveData()
    {
        return new Dictionary
        {
            {"exploded", exploded}
        };
    }

    public void LoadData(Dictionary savedData)
    {
        exploded = System.Convert.ToBoolean(savedData["exploded"]);
        if (!exploded) return;
        var wallsAnim = GetNode<AnimationPlayer>("../wallparts/anim");
        wallsAnim.Play("explode");
        wallsAnim.Seek(wallsAnim.CurrentAnimationLength, true);
        GetNode<CollisionShape>("../WallShape").Disabled = true;
    }
}
