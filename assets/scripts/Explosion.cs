using Godot;
using Godot.Collections;

public partial class Explosion : Node3D, ISavable
{
    public bool checkWalls = true;

    private Array<GpuParticles3D> parts = [];
    private AnimationPlayer anim;
    private AudioStreamPlayer3D audi;

    private bool exploded;

    public override void _Ready()
    {
        anim = GetNode<AnimationPlayer>("anim");
        audi = GetNode<AudioStreamPlayer3D>("audi");
        var partsParent = GetNode("parts");
        foreach (var node in partsParent.GetChildren())
        {
            var tempPart = (GpuParticles3D)node;
            parts.Add(tempPart);
        }
    }

    public async void Explode()
    {
        anim.Play("explode");
        audi.Play();

        if (checkWalls)
        {
            GetNode<AnimationPlayer>("../wallparts/anim").Play("explode");
            GetNode<CollisionShape3D>("../WallShape").Disabled = true;
        }
       
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
            {"exploded", exploded},
            {"checkWalls", checkWalls},
        };
    }

    public void LoadData(Dictionary savedData)
    {
        exploded = System.Convert.ToBoolean(savedData["exploded"]);
        if (savedData.TryGetValue("checkWalls", out var checkWallsValue))
        {
            checkWalls = checkWallsValue.AsBool();
        }

        if (!exploded) return;
        if (!checkWalls) return;

        var wallsAnim = GetNode<AnimationPlayer>("../wallparts/anim");
        wallsAnim.Play("explode");
        wallsAnim.Seek(wallsAnim.CurrentAnimationLength, true);
        GetNode<CollisionShape3D>("../WallShape").Disabled = true;
    }
}
